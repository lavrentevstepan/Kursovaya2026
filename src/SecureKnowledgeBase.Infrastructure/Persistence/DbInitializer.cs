using Microsoft.EntityFrameworkCore;
using SecureKnowledgeBase.Domain.Entities;
using SecureKnowledgeBase.Domain.Enums;
using SecureKnowledgeBase.Infrastructure.Services;

namespace SecureKnowledgeBase.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        await context.Database.EnsureCreatedAsync(cancellationToken);

        if (!await context.Users.AnyAsync(cancellationToken))
        {
            var adminUser = new User
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Username = "admin",
                DisplayName = "Administrator",
                PasswordHash = LocalAuthenticationService.HashPassword("AdminPass123!"),
                Role = UserRole.SystemAdmin,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            var auditorUser = new User
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Username = "auditor",
                DisplayName = "Security Auditor",
                PasswordHash = LocalAuthenticationService.HashPassword("AuditorPass123!"),
                Role = UserRole.SecurityAuditor,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            var regularUser = new User
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Username = "user",
                DisplayName = "Regular User",
                PasswordHash = LocalAuthenticationService.HashPassword("UserPass123!"),
                Role = UserRole.User,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            await context.Users.AddRangeAsync([adminUser, auditorUser, regularUser], cancellationToken);

            var initAuditEvent = new AuditEvent
            {
                Id = Guid.NewGuid(),
                TimestampUtc = DateTime.UtcNow,
                Username = "system",
                Action = AuditAction.AdminAction,
                TargetType = "Database",
                TargetId = "InitialSeed",
                Result = "Success",
                MetadataJson = "{\"action\":\"database_initialized\",\"seededUsers\":[\"admin\",\"auditor\",\"user\"]}",
                ClientIpAddress = "127.0.0.1"
            };

            await context.AuditEvents.AddAsync(initAuditEvent, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
