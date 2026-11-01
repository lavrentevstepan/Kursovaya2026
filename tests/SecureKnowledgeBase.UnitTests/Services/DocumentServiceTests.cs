using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using SecureKnowledgeBase.Application.DTOs;
using SecureKnowledgeBase.Application.Interfaces;
using SecureKnowledgeBase.Domain.Enums;
using SecureKnowledgeBase.Domain.Interfaces;
using SecureKnowledgeBase.Infrastructure.Configuration;
using SecureKnowledgeBase.Infrastructure.Persistence;
using SecureKnowledgeBase.Infrastructure.Repositories;
using SecureKnowledgeBase.Infrastructure.Services;
using SecureKnowledgeBase.Infrastructure.Services.Extractors;
using SecureKnowledgeBase.UnitTests.Common;

namespace SecureKnowledgeBase.UnitTests.Services;

public class DocumentServiceTests : IAsyncLifetime
{
    private AppDbContext _context = null!;
    private SqliteConnection _connection = null!;
    private string _tempStoragePath = null!;
    private LocalFileStorageService _fileStorageService = null!;
    private AuditService _auditService = null!;
    private DocumentService _documentService = null!;
    private Guid _adminUserId;

    public async Task InitializeAsync()
    {
        (_context, _connection) = await TestDbContextFactory.CreateInitializedContextAsync();

        _tempStoragePath = Path.Combine(Path.GetTempPath(), "skb_doc_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempStoragePath);

        var options = Options.Create(new SecurityOptions
        {
            StorageDirectory = _tempStoragePath,
            MaxFileSizeMb = 20,
            AllowedExtensions = new[] { ".txt", ".md", ".pdf", ".docx" }
        });

        _fileStorageService = new LocalFileStorageService(options);
        var auditRepo = new AuditRepository(_context);
        _auditService = new AuditService(auditRepo);
        var userRepo = new UserRepository(_context);
        var docRepo = new DocumentRepository(_context);

        var compositeExtractor = new CompositeTextExtractor(new ITextExtractor[]
        {
            new PlainTextExtractor(),
            new DocxTextExtractor(),
            new PdfTextExtractor()
        });
        var chunker = new TextChunker();
        var indexingService = new DocumentIndexingService(docRepo, _fileStorageService, compositeExtractor, chunker, _auditService);

        _documentService = new DocumentService(docRepo, _fileStorageService, _auditService, userRepo, indexingService, options);

        var admin = await userRepo.GetByUsernameAsync("admin");
        _adminUserId = admin!.Id;
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
    public async Task UploadDocumentAsync_ValidFile_SucceedsAndPersists()
    {
        var text = "DevSecOps Variant 25 Security Policy Details";
        var bytes = Encoding.UTF8.GetBytes(text);
        using var stream = new MemoryStream(bytes);

        var request = new DocumentUploadRequestDto
        {
            Title = "Политика безопасности",
            Description = "Регламент информационной безопасности",
            Category = "Безопасность",
            AccessLevel = AccessLevel.Restricted,
            OriginalFileName = "security_policy.md",
            ContentType = "text/markdown",
            FileStream = stream,
            FileLength = bytes.Length
        };

        var result = await _documentService.UploadDocumentAsync(request, _adminUserId);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Политика безопасности", result.Title);
        Assert.Equal("Безопасность", result.Category);
        Assert.Equal(AccessLevel.Restricted, result.AccessLevel);
        Assert.Equal(DocumentStatus.Indexed, result.Status);
        Assert.True(result.ChunkCount > 0);
        Assert.NotEmpty(result.FileSha256Hash);

        var loaded = await _documentService.GetDocumentByIdAsync(result.Id, AccessLevel.Restricted);
        Assert.NotNull(loaded);
        Assert.Equal(result.Id, loaded.Id);
    }

    [Fact]
    public async Task UploadDocumentAsync_DisallowedExtension_ThrowsArgumentException()
    {
        var bytes = Encoding.UTF8.GetBytes("fake executable");
        using var stream = new MemoryStream(bytes);

        var request = new DocumentUploadRequestDto
        {
            Title = "Вредоносный файл",
            OriginalFileName = "script.exe",
            ContentType = "application/x-msdownload",
            FileStream = stream,
            FileLength = bytes.Length
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _documentService.UploadDocumentAsync(request, _adminUserId));
    }

    [Fact]
    public async Task UploadDocumentAsync_ExceedsMaxFileSize_ThrowsArgumentException()
    {
        using var stream = new MemoryStream(new byte[100]);

        var request = new DocumentUploadRequestDto
        {
            Title = "Огромный файл",
            OriginalFileName = "large.pdf",
            ContentType = "application/pdf",
            FileStream = stream,
            FileLength = 25L * 1024 * 1024
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _documentService.UploadDocumentAsync(request, _adminUserId));
    }

    [Fact]
    public async Task GetDocumentByIdAsync_InsufficientAccessLevel_ReturnsNull()
    {
        var bytes = Encoding.UTF8.GetBytes("Confidential server architecture details");
        using var stream = new MemoryStream(bytes);

        var request = new DocumentUploadRequestDto
        {
            Title = "Архитектура ядра",
            OriginalFileName = "arch.txt",
            AccessLevel = AccessLevel.Confidential,
            FileStream = stream,
            FileLength = bytes.Length
        };

        var uploaded = await _documentService.UploadDocumentAsync(request, _adminUserId);

        var restrictedView = await _documentService.GetDocumentByIdAsync(uploaded.Id, AccessLevel.PublicInternal);
        Assert.Null(restrictedView);

        var allowedView = await _documentService.GetDocumentByIdAsync(uploaded.Id, AccessLevel.Confidential);
        Assert.NotNull(allowedView);
    }

    [Fact]
    public async Task UpdateDocumentMetadataAsync_Authorized_UpdatesMetadata()
    {
        var bytes = Encoding.UTF8.GetBytes("Initial text");
        using var stream = new MemoryStream(bytes);

        var request = new DocumentUploadRequestDto
        {
            Title = "Старое название",
            Description = "Старое описание",
            Category = "Общие",
            AccessLevel = AccessLevel.PublicInternal,
            OriginalFileName = "doc.txt",
            FileStream = stream,
            FileLength = bytes.Length
        };

        var uploaded = await _documentService.UploadDocumentAsync(request, _adminUserId);

        var updateDto = new DocumentUpdateDto
        {
            Title = "Новое название",
            Description = "Обновленное описание",
            Category = "ИТ-Регламенты",
            AccessLevel = AccessLevel.Restricted
        };

        var updated = await _documentService.UpdateDocumentMetadataAsync(uploaded.Id, updateDto, _adminUserId, UserRole.SystemAdmin);

        Assert.NotNull(updated);
        Assert.Equal("Новое название", updated.Title);
        Assert.Equal("Обновленное описание", updated.Description);
        Assert.Equal("ИТ-Регламенты", updated.Category);
        Assert.Equal(AccessLevel.Restricted, updated.AccessLevel);
        Assert.NotNull(updated.UpdatedAtUtc);
    }

    [Fact]
    public async Task DeleteDocumentAsync_Authorized_DeletesDocumentAndFile()
    {
        var bytes = Encoding.UTF8.GetBytes("Content to be removed");
        using var stream = new MemoryStream(bytes);

        var request = new DocumentUploadRequestDto
        {
            Title = "Удаляемый документ",
            OriginalFileName = "delete_me.txt",
            FileStream = stream,
            FileLength = bytes.Length
        };

        var uploaded = await _documentService.UploadDocumentAsync(request, _adminUserId);

        var deleted = await _documentService.DeleteDocumentAsync(uploaded.Id, _adminUserId, UserRole.SystemAdmin);
        Assert.True(deleted);

        var loaded = await _documentService.GetDocumentByIdAsync(uploaded.Id, AccessLevel.Confidential);
        Assert.Null(loaded);
    }
}
