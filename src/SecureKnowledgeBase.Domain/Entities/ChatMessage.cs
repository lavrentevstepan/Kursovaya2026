using SecureKnowledgeBase.Domain.Enums;

namespace SecureKnowledgeBase.Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChatSessionId { get; set; }
    public ChatSession? ChatSession { get; set; }
    public string QueryText { get; set; } = string.Empty;
    public string ResponseText { get; set; } = string.Empty;
    public ChatResponseMode ResponseMode { get; set; } = ChatResponseMode.Extractive;
    public string SourcesJson { get; set; } = string.Empty;
    public long ExecutionTimeMs { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
