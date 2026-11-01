using SecureKnowledgeBase.Domain.Enums;
using SecureKnowledgeBase.Infrastructure.Services;
using Xunit;

namespace SecureKnowledgeBase.UnitTests.Services;

public class CurrentUserServiceTests
{
    [Fact]
    public void UnauthenticatedUser_ShouldHaveDefaultValues()
    {
        var service = new CurrentUserService();

        Assert.False(service.IsAuthenticated);
        Assert.Null(service.UserId);
        Assert.Null(service.Username);
        Assert.Null(service.Role);
        Assert.Equal(AccessLevel.PublicInternal, service.MaxAllowedAccessLevel);
    }

    [Theory]
    [InlineData(UserRole.SystemAdmin, AccessLevel.Confidential)]
    [InlineData(UserRole.SecurityAuditor, AccessLevel.Confidential)]
    [InlineData(UserRole.KnowledgeAdmin, AccessLevel.Restricted)]
    [InlineData(UserRole.User, AccessLevel.PublicInternal)]
    public void AuthenticatedUser_ShouldHaveProperMaxAllowedAccessLevel(UserRole role, AccessLevel expectedAccessLevel)
    {
        var service = new CurrentUserService();
        service.SetUser(Guid.NewGuid(), "testuser", "Test User", role);

        Assert.True(service.IsAuthenticated);
        Assert.Equal(role, service.Role);
        Assert.Equal(expectedAccessLevel, service.MaxAllowedAccessLevel);
    }

    [Fact]
    public void ClearUser_ShouldResetState()
    {
        var service = new CurrentUserService();
        service.SetUser(Guid.NewGuid(), "testuser", "Test User", UserRole.User);
        service.ClearUser();

        Assert.False(service.IsAuthenticated);
        Assert.Null(service.UserId);
    }
}
