using SecureKnowledgeBase.Domain.Enums;
using SecureKnowledgeBase.Domain.Interfaces;

namespace SecureKnowledgeBase.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    public event Action? OnChange;

    public Guid? UserId { get; private set; }
    public string? Username { get; private set; }
    public string? DisplayName { get; private set; }
    public UserRole? Role { get; private set; }
    public bool IsAuthenticated => UserId.HasValue;

    public AccessLevel MaxAllowedAccessLevel
    {
        get
        {
            if (!Role.HasValue) return AccessLevel.PublicInternal;
            return Role.Value switch
            {
                UserRole.SystemAdmin => AccessLevel.Confidential,
                UserRole.SecurityAuditor => AccessLevel.Confidential,
                UserRole.KnowledgeAdmin => AccessLevel.Restricted,
                _ => AccessLevel.PublicInternal
            };
        }
    }

    public void SetUser(Guid userId, string username, string displayName, UserRole role)
    {
        UserId = userId;
        Username = username;
        DisplayName = displayName;
        Role = role;
        NotifyStateChanged();
    }

    public void ClearUser()
    {
        UserId = null;
        Username = null;
        DisplayName = null;
        Role = null;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
