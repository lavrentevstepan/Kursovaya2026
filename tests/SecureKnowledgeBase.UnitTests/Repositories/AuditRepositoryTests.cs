using SecureKnowledgeBase.Domain.Entities;
using SecureKnowledgeBase.Domain.Enums;
using SecureKnowledgeBase.Infrastructure.Repositories;
using SecureKnowledgeBase.UnitTests.Common;
using Xunit;

namespace SecureKnowledgeBase.UnitTests.Repositories;

public class AuditRepositoryTests
{
    [Fact]
    public async Task AddAndFilterAuditEvents_ShouldWorkCorrectly()
    {
        var (context, connection) = await TestDbContextFactory.CreateInitializedContextAsync();
        using (connection)
        using (context)
        {
            var auditRepo = new AuditRepository(context);

            var event1 = new AuditEvent
            {
                Username = "admin",
                Action = AuditAction.Login,
                TargetType = "User",
                TargetId = "admin",
                Result = "Success",
                ClientIpAddress = "192.168.1.50"
            };

            var event2 = new AuditEvent
            {
                Username = "user",
                Action = AuditAction.AssistantQuery,
                TargetType = "Chat",
                TargetId = "session_1",
                Result = "Success",
                ClientIpAddress = "192.168.1.55"
            };

            await auditRepo.AddAsync(event1);
            await auditRepo.AddAsync(event2);

            var adminEvents = await auditRepo.GetFilteredAsync(username: "admin");
            Assert.Single(adminEvents);
            Assert.Equal(AuditAction.Login, adminEvents[0].Action);

            var queryEvents = await auditRepo.GetFilteredAsync(action: AuditAction.AssistantQuery);
            Assert.Single(queryEvents);
            Assert.Equal("user", queryEvents[0].Username);
        }
    }
}
