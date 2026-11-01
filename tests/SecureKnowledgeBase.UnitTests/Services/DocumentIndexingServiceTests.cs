using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using SecureKnowledgeBase.Domain.Entities;
using SecureKnowledgeBase.Domain.Enums;
using SecureKnowledgeBase.Domain.Interfaces;
using SecureKnowledgeBase.Infrastructure.Configuration;
using SecureKnowledgeBase.Infrastructure.Persistence;
using SecureKnowledgeBase.Infrastructure.Repositories;
using SecureKnowledgeBase.Infrastructure.Services;
using SecureKnowledgeBase.Infrastructure.Services.Extractors;
using SecureKnowledgeBase.UnitTests.Common;

namespace SecureKnowledgeBase.UnitTests.Services;

public class DocumentIndexingServiceTests : IAsyncLifetime
{
    private AppDbContext _context = null!;
    private SqliteConnection _connection = null!;
    private string _tempStoragePath = null!;
    private LocalFileStorageService _storageService = null!;
    private DocumentIndexingService _indexingService = null!;
    private DocumentRepository _docRepo = null!;
    private Guid _adminId;

    public async Task InitializeAsync()
    {
        (_context, _connection) = await TestDbContextFactory.CreateInitializedContextAsync();

        _tempStoragePath = Path.Combine(Path.GetTempPath(), "skb_idx_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempStoragePath);

        var options = Options.Create(new SecurityOptions
        {
            StorageDirectory = _tempStoragePath
        });

        _storageService = new LocalFileStorageService(options);
        _docRepo = new DocumentRepository(_context);
        var auditRepo = new AuditRepository(_context);
        var auditService = new AuditService(auditRepo);

        var compositeExtractor = new CompositeTextExtractor(new ITextExtractor[]
        {
            new PlainTextExtractor(),
            new DocxTextExtractor(),
            new PdfTextExtractor()
        });

        var chunker = new TextChunker();

        _indexingService = new DocumentIndexingService(
            _docRepo,
            _storageService,
            compositeExtractor,
            chunker,
            auditService);

        var admin = _context.Users.First(u => u.Username == "admin");
        _adminId = admin.Id;
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();

        if (Directory.Exists(_tempStoragePath))
        {
            try
            {
                Directory.Delete(_tempStoragePath, true);
            }
            catch
            {
            }
        }
    }

    [Fact]
    public async Task IndexDocumentAsync_ValidTextDocument_CreatesChunksAndSetsIndexedStatus()
    {
        var text = @"# Стандарт сетевой безопасности
Сетевые экраны должны фильтровать весь входящий и исходящий трафик.
Порты баз данных не должны публиковаться в публичный интернет.";

        var bytes = Encoding.UTF8.GetBytes(text);
        using var stream = new MemoryStream(bytes);

        var (storedName, hash, size) = await _storageService.SaveFileAsync(stream, "net_sec.md");

        var doc = new Document
        {
            Id = Guid.NewGuid(),
            Title = "Стандарт сетевой безопасности",
            Description = "Регламент межсетевого экранирования",
            Category = "Сеть",
            AccessLevel = AccessLevel.PublicInternal,
            Status = DocumentStatus.Uploaded,
            OwnerId = _adminId,
            OriginalFileName = "net_sec.md",
            StoredFileName = storedName,
            FileSha256Hash = hash,
            FileSizeBytes = size
        };

        var job = new ProcessingJob
        {
            Id = Guid.NewGuid(),
            DocumentId = doc.Id,
            JobType = "IngestAndIndex",
            Status = ProcessingJobStatus.Pending
        };
        doc.ProcessingJobs.Add(job);

        await _docRepo.AddAsync(doc);

        var indexedChunks = await _indexingService.IndexDocumentAsync(doc.Id);

        Assert.True(indexedChunks > 0);

        var updatedDoc = await _docRepo.GetByIdAsync(doc.Id);
        Assert.NotNull(updatedDoc);
        Assert.Equal(DocumentStatus.Indexed, updatedDoc.Status);

        var chunks = await _docRepo.GetChunksByDocumentIdAsync(doc.Id);
        Assert.NotEmpty(chunks);
        Assert.True(chunks.All(c => c.IsIndexed));
    }
}
