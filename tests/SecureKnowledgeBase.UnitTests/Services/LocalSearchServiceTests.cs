using Microsoft.Data.Sqlite;
using SecureKnowledgeBase.Application.DTOs;
using SecureKnowledgeBase.Domain.Entities;
using SecureKnowledgeBase.Domain.Enums;
using SecureKnowledgeBase.Infrastructure.Persistence;
using SecureKnowledgeBase.Infrastructure.Repositories;
using SecureKnowledgeBase.Infrastructure.Services;
using SecureKnowledgeBase.UnitTests.Common;

namespace SecureKnowledgeBase.UnitTests.Services;

public class LocalSearchServiceTests : IAsyncLifetime
{
    private AppDbContext _context = null!;
    private SqliteConnection _connection = null!;
    private LocalSearchService _searchService = null!;

    public async Task InitializeAsync()
    {
        (_context, _connection) = await TestDbContextFactory.CreateInitializedContextAsync();

        var auditRepo = new AuditRepository(_context);
        var auditService = new AuditService(auditRepo);
        _searchService = new LocalSearchService(_context, auditService);

        var adminUser = _context.Users.First(u => u.Username == "admin");

        var publicDoc = new Document
        {
            Id = Guid.NewGuid(),
            Title = "Политика парольной защиты",
            Description = "Регламент создания и смены паролей пользователей",
            Category = "Безопасность",
            AccessLevel = AccessLevel.PublicInternal,
            Status = DocumentStatus.Indexed,
            OwnerId = adminUser.Id,
            OriginalFileName = "passwords.txt",
            StoredFileName = "passwords.txt",
            FileSha256Hash = "hash1"
        };

        var confidentialDoc = new Document
        {
            Id = Guid.NewGuid(),
            Title = "Секретные ключи инфраструктуры",
            Description = "Параметры закрытых шлюзов и приватные ключи",
            Category = "ИТ",
            AccessLevel = AccessLevel.Confidential,
            Status = DocumentStatus.Indexed,
            OwnerId = adminUser.Id,
            OriginalFileName = "keys.txt",
            StoredFileName = "keys.txt",
            FileSha256Hash = "hash2"
        };

        _context.Documents.AddRange(publicDoc, confidentialDoc);

        var chunk1 = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = publicDoc.Id,
            ChunkNumber = 1,
            SectionHeader = "Требования к паролям",
            TextContent = "Все пользователи обязаны использовать сложные пароли длиной от двенадцати знаков.",
            IsIndexed = true
        };

        var chunk2 = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = confidentialDoc.Id,
            ChunkNumber = 1,
            SectionHeader = "Ключи шифрования",
            TextContent = "Закрытый приватный ключ корневого центра сертификации хранится в аппаратном модуле.",
            IsIndexed = true
        };

        _context.DocumentChunks.AddRange(chunk1, chunk2);
        await _context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task SearchAsync_MatchingQuery_ReturnsRankedResult()
    {
        var request = new SearchRequestDto
        {
            Query = "пароли",
            MaxResults = 5
        };

        var result = await _searchService.SearchAsync(request, AccessLevel.PublicInternal);

        Assert.NotNull(result);
        Assert.True(result.TotalHits > 0);
        Assert.Equal("Политика парольной защиты", result.Hits[0].DocumentTitle);
        Assert.NotEmpty(result.Hits[0].Snippet);
    }

    [Fact]
    public async Task SearchAsync_AccessLevelFiltering_HidesConfidentialDocumentsFromPublicUsers()
    {
        var request = new SearchRequestDto
        {
            Query = "приватный ключ",
            MaxResults = 5
        };

        var publicResult = await _searchService.SearchAsync(request, AccessLevel.PublicInternal);
        Assert.Equal(0, publicResult.TotalHits);

        var confidentialResult = await _searchService.SearchAsync(request, AccessLevel.Confidential);
        Assert.True(confidentialResult.TotalHits > 0);
        Assert.Equal("Секретные ключи инфраструктуры", confidentialResult.Hits[0].DocumentTitle);
    }

    [Fact]
    public async Task SearchAsync_CategoryFiltering_RestrictsResults()
    {
        var request = new SearchRequestDto
        {
            Query = "пароли",
            Category = "ИТ",
            MaxResults = 5
        };

        var result = await _searchService.SearchAsync(request, AccessLevel.PublicInternal);
        Assert.Equal(0, result.TotalHits);
    }

    [Fact]
    public async Task SearchAsync_EmptyQuery_ReturnsZeroHits()
    {
        var request = new SearchRequestDto
        {
            Query = "   "
        };

        var result = await _searchService.SearchAsync(request, AccessLevel.Confidential);
        Assert.Equal(0, result.TotalHits);
    }
}
