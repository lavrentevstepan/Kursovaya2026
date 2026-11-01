using SecureKnowledgeBase.Domain.Enums;

namespace SecureKnowledgeBase.Domain.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Username { get; }
    string? DisplayName { get; }
    UserRole? Role { get; }
    bool IsAuthenticated { get; }
    AccessLevel MaxAllowedAccessLevel { get; }
}
