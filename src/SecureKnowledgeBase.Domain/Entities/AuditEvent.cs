using SecureKnowledgeBase.Domain.Enums;

namespace SecureKnowledgeBase.Domain.Entities;

public class AuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string Username { get; set; } = string.Empty;
    public AuditAction Action { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = string.Empty;
    public string ClientIpAddress { get; set; } = string.Empty;
}
