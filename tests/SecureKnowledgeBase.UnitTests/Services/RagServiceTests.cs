using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using SecureKnowledgeBase.Application.DTOs;
using SecureKnowledgeBase.Application.Interfaces;
using SecureKnowledgeBase.Domain.Entities;
using SecureKnowledgeBase.Domain.Enums;
using SecureKnowledgeBase.Infrastructure.Configuration;
using SecureKnowledgeBase.Infrastructure.Persistence;
using SecureKnowledgeBase.Infrastructure.Repositories;
using SecureKnowledgeBase.Infrastructure.Services;
using SecureKnowledgeBase.UnitTests.Common;

namespace SecureKnowledgeBase.UnitTests.Services;

public class RagServiceTests : IAsyncLifetime
{
    private AppDbContext _context = null!;
    private SqliteConnection _connection = null!;
    private RagService _ragService = null!;
    private Guid _adminId;
    private Guid _userId;

    public async Task InitializeAsync()
    {
        (_context, _connection) = await TestDbContextFactory.CreateInitializedContextAsync();

        var admin = await _context.Users.FindAsync(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        _adminId = admin!.Id;

        var user = await _context.Users.FindAsync(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        _userId = user!.Id;

        var doc1 = new Document
        {
            Id = Guid.NewGuid(),
            Title = "Политика парольной безопасности",
            Description = "Регламент длины и сложности паролей",
            Category = "Безопасность",
            AccessLevel = AccessLevel.PublicInternal,
            Status = DocumentStatus.Indexed,
            OwnerId = _adminId,
            CreatedAtUtc = DateTime.UtcNow,
            OriginalFileName = "passwords.md",
            StoredFileName = "passwords.md",
            ContentType = "text/markdown",
            FileSizeBytes = 100,
            FileSha256Hash = "hash1"
        };

        doc1.Chunks.Add(new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = doc1.Id,
            ChunkNumber = 1,
            TextContent = "Минимальная длина пароля сотрудника составляет не менее 12 символов. Пароль должен обновляться каждые 90 дней.",
            SectionHeader = "Требования к паролям",
            CreatedAtUtc = DateTime.UtcNow
        });

        var doc2 = new Document
        {
            Id = Guid.NewGuid(),
            Title = "Архитектура серверов и баз данных",
            Description = "Конфиденциальные правила доступа к серверам",
            Category = "Инфраструктура",
            AccessLevel = AccessLevel.Confidential,
            Status = DocumentStatus.Indexed,
            OwnerId = _adminId,
            CreatedAtUtc = DateTime.UtcNow,
            OriginalFileName = "servers.md",
            StoredFileName = "servers.md",
            ContentType = "text/markdown",
            FileSizeBytes = 100,
            FileSha256Hash = "hash2"
        };

        doc2.Chunks.Add(new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = doc2.Id,
            ChunkNumber = 1,
            TextContent = "Подключение к серверам СУБД возможно исключительно через Bastion хост 10.20.0.5 по ключам ed25519. Вход root запрещен.",
            SectionHeader = "Управление серверами",
            CreatedAtUtc = DateTime.UtcNow
        });

        _context.Documents.AddRange(doc1, doc2);
        await _context.SaveChangesAsync();

        var options = Options.Create(new SecurityOptions
        {
            EnableLocalLlm = false,
            OllamaEndpoint = "http://localhost:11434",
            OllamaModel = "qwen2.5:1.5b"
        });

        var auditRepo = new AuditRepository(_context);
        var auditService = new AuditService(auditRepo);
        var searchService = new LocalSearchService(_context, auditService);
        var fakeHttpClient = new HttpClient();
        var llmClient = new OllamaLlmClient(fakeHttpClient, options);
        var extractiveQaService = new ExtractiveQaService();

        _ragService = new RagService(searchService, llmClient, extractiveQaService, auditService, options);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task AskAsync_PublicUser_CanRetrievePasswordPolicy()
    {
        var request = new RagQueryRequestDto
        {
            Question = "Какая минимальная длина пароля?",
            Mode = RagMode.Auto
        };

        var response = await _ragService.AskAsync(request, _userId, "user", AccessLevel.PublicInternal);

        Assert.True(response.HasFoundAnswer);
        Assert.Contains("12 символов", response.Answer);
        Assert.NotEmpty(response.Sources);
        Assert.Equal(RagMode.ExtractiveOnly, response.ModeUsed);
    }

    [Fact]
    public async Task AskAsync_PublicUser_CannotRetrieveConfidentialServerData()
    {
        var request = new RagQueryRequestDto
        {
            Question = "Как подключиться к серверам через Bastion хост?",
            Mode = RagMode.Auto
        };

        var response = await _ragService.AskAsync(request, _userId, "user", AccessLevel.PublicInternal);

        Assert.False(response.HasFoundAnswer);
        Assert.Contains("не найден", response.Answer);
        Assert.Empty(response.Sources);
    }

    [Fact]
    public async Task AskAsync_AdminUser_CanRetrieveConfidentialServerData()
    {
        var request = new RagQueryRequestDto
        {
            Question = "Как подключиться к серверам через Bastion хост?",
            Mode = RagMode.Auto
        };

        var response = await _ragService.AskAsync(request, _adminId, "admin", AccessLevel.Confidential);

        Assert.True(response.HasFoundAnswer);
        Assert.Contains("Bastion", response.Answer);
        Assert.NotEmpty(response.Sources);
    }

    [Fact]
    public async Task AskAsync_UnansweredQuestion_ReturnsNotFoundResponse()
    {
        var request = new RagQueryRequestDto
        {
            Question = "Какова масса Луны в килограммах?",
            Mode = RagMode.Auto
        };

        var response = await _ragService.AskAsync(request, _userId, "user", AccessLevel.PublicInternal);

        Assert.False(response.HasFoundAnswer);
        Assert.Contains("не найден", response.Answer);
    }

    [Fact]
    public async Task AskAsync_LogsAssistantQueryAuditEvent()
    {
        var request = new RagQueryRequestDto
        {
            Question = "Какая длина пароля?",
            Mode = RagMode.ExtractiveOnly
        };

        await _ragService.AskAsync(request, _userId, "user", AccessLevel.PublicInternal);

        var auditEvent = _context.AuditEvents.FirstOrDefault(e => e.Action == AuditAction.AssistantQuery && e.TargetType == "RAG-Assistant");
        Assert.NotNull(auditEvent);
        Assert.Equal("user", auditEvent.Username);
        Assert.Equal("RAG-Assistant", auditEvent.TargetType);
    }
}
