using SecureKnowledgeBase.Domain.Enums;

namespace SecureKnowledgeBase.Domain.Entities;

public class ChatSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public ChatResponseMode ResponseMode { get; set; } = ChatResponseMode.Extractive;

    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
