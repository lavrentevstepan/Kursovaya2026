using SecureKnowledgeBase.Domain.Enums;

namespace SecureKnowledgeBase.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAtUtc { get; set; }

    public ICollection<Document> OwnedDocuments { get; set; } = new List<Document>();
    public ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
}
