using SecureKnowledgeBase.Application.DTOs;
using SecureKnowledgeBase.Domain.Enums;
using SecureKnowledgeBase.Infrastructure.Repositories;
using SecureKnowledgeBase.Infrastructure.Services;
using SecureKnowledgeBase.UnitTests.Common;
using Xunit;

namespace SecureKnowledgeBase.UnitTests.Services;

public class LocalAuthenticationServiceTests
{
    [Theory]
    [InlineData("admin", "AdminPass123!", UserRole.SystemAdmin)]
    [InlineData("auditor", "AuditorPass123!", UserRole.SecurityAuditor)]
    [InlineData("user", "UserPass123!", UserRole.User)]
    public async Task AuthenticateAsync_WithValidCredentials_ShouldSucceed(string username, string password, UserRole expectedRole)
    {
        var (context, connection) = await TestDbContextFactory.CreateInitializedContextAsync();
        using (connection)
        using (context)
        {
            var userRepo = new UserRepository(context);
            var service = new LocalAuthenticationService(userRepo);

            var result = await service.AuthenticateAsync(new LoginRequestDto
            {
                Username = username,
                Password = password
            });

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.User);
            Assert.Equal(expectedRole, result.User.Role);
            Assert.False(string.IsNullOrEmpty(result.Token));
        }
    }

    [Fact]
    public async Task AuthenticateAsync_WithInvalidPassword_ShouldFail()
    {
        var (context, connection) = await TestDbContextFactory.CreateInitializedContextAsync();
        using (connection)
        using (context)
        {
            var userRepo = new UserRepository(context);
            var service = new LocalAuthenticationService(userRepo);

            var result = await service.AuthenticateAsync(new LoginRequestDto
            {
                Username = "admin",
                Password = "WrongPassword999!"
            });

            Assert.False(result.IsSuccess);
            Assert.Null(result.User);
            Assert.Equal("Неверный пароль", result.ErrorMessage);
        }
    }

    [Fact]
    public async Task AuthenticateAsync_WithUnknownUser_ShouldFail()
    {
        var (context, connection) = await TestDbContextFactory.CreateInitializedContextAsync();
        using (connection)
        using (context)
        {
            var userRepo = new UserRepository(context);
            var service = new LocalAuthenticationService(userRepo);

            var result = await service.AuthenticateAsync(new LoginRequestDto
            {
                Username = "nonexistent_user",
                Password = "SomePassword123!"
            });

            Assert.False(result.IsSuccess);
            Assert.Equal("Пользователь не найден", result.ErrorMessage);
        }
    }

    [Fact]
    public void HashPassword_ShouldBeDeterministic()
    {
        var hash1 = LocalAuthenticationService.HashPassword("TestSecret");
        var hash2 = LocalAuthenticationService.HashPassword("TestSecret");

        Assert.Equal(hash1, hash2);
        Assert.NotEmpty(hash1);
    }
}
