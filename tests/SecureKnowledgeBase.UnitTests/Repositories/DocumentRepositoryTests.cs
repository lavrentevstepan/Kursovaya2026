using SecureKnowledgeBase.Domain.Entities;
using SecureKnowledgeBase.Domain.Enums;
using SecureKnowledgeBase.Infrastructure.Repositories;
using SecureKnowledgeBase.UnitTests.Common;
using Xunit;

namespace SecureKnowledgeBase.UnitTests.Repositories;

public class DocumentRepositoryTests
{
    [Fact]
    public async Task AddAndGetDocument_ShouldPersistDocumentAndChunks()
    {
        var (context, connection) = await TestDbContextFactory.CreateInitializedContextAsync();
        using (connection)
        using (context)
        {
            var userRepo = new UserRepository(context);
            var admin = await userRepo.GetByUsernameAsync("admin");
            Assert.NotNull(admin);

            var docRepo = new DocumentRepository(context);
            var document = new Document
            {
                Title = "Политика безопасности",
                Description = "Базовые правила ИБ организации",
                Category = "Безопасность",
                AccessLevel = AccessLevel.Restricted,
                Status = DocumentStatus.Indexed,
                OwnerId = admin.Id,
                OriginalFileName = "security_policy.pdf",
                StoredFileName = "stored_sec_123.bin",
                ContentType = "application/pdf",
                FileSizeBytes = 1048576,
                FileSha256Hash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"
            };

            await docRepo.AddAsync(document);

            var chunk1 = new DocumentChunk
            {
                DocumentId = document.Id,
                ChunkNumber = 1,
                SectionHeader = "Введение",
                TextContent = "Настоящая политика регулирует пароли и уровни доступа к ресурсам.",
                IsIndexed = true
            };

            var chunk2 = new DocumentChunk
            {
                DocumentId = document.Id,
                ChunkNumber = 2,
                SectionHeader = "Аудит",
                TextContent = "Все попытки входа и удаления документов протоколируются.",
                IsIndexed = true
            };

            await docRepo.AddChunksAsync([chunk1, chunk2]);

            var fetchedDoc = await docRepo.GetByIdAsync(document.Id);
            Assert.NotNull(fetchedDoc);
            Assert.Equal("Политика безопасности", fetchedDoc.Title);
            Assert.Equal("admin", fetchedDoc.Owner?.Username);

            var fetchedChunks = await docRepo.GetChunksByDocumentIdAsync(document.Id);
            Assert.Equal(2, fetchedChunks.Count);
            Assert.Equal("Введение", fetchedChunks[0].SectionHeader);
        }
    }

    [Fact]
    public async Task GetAllAsync_WithMaxAccessLevel_ShouldFilterProperly()
    {
        var (context, connection) = await TestDbContextFactory.CreateInitializedContextAsync();
        using (connection)
        using (context)
        {
            var userRepo = new UserRepository(context);
            var admin = await userRepo.GetByUsernameAsync("admin");
            Assert.NotNull(admin);

            var docRepo = new DocumentRepository(context);

            var publicDoc = new Document
            {
                Title = "Общий регламент",
                Category = "Общие",
                AccessLevel = AccessLevel.PublicInternal,
                Status = DocumentStatus.Indexed,
                OwnerId = admin.Id,
                OriginalFileName = "public.txt",
                StoredFileName = "pub.bin",
                ContentType = "text/plain",
                FileSha256Hash = "abc1"
            };

            var confidentialDoc = new Document
            {
                Title = "Конфиденциальный план",
                Category = "Секретно",
                AccessLevel = AccessLevel.Confidential,
                Status = DocumentStatus.Indexed,
                OwnerId = admin.Id,
                OriginalFileName = "conf.txt",
                StoredFileName = "conf.bin",
                ContentType = "text/plain",
                FileSha256Hash = "abc2"
            };

            await docRepo.AddAsync(publicDoc);
            await docRepo.AddAsync(confidentialDoc);

            var publicOnly = await docRepo.GetAllAsync(AccessLevel.PublicInternal);
            Assert.Single(publicOnly);
            Assert.Equal("Общий регламент", publicOnly[0].Title);

            var allForAdmin = await docRepo.GetAllAsync(AccessLevel.Confidential);
            Assert.Equal(2, allForAdmin.Count);
        }
    }
}
