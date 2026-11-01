using SecureKnowledgeBase.Domain.Enums;

namespace SecureKnowledgeBase.Application.DTOs;

public class AuditEventDto
{
    public Guid Id { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string Username { get; set; } = string.Empty;
    public AuditAction Action { get; set; }
    public string ActionName => Action.ToString();
    public string TargetType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = string.Empty;
    public string ClientIpAddress { get; set; } = string.Empty;
}

public class AuditFilterDto
{
    public string? Username { get; set; }
    public AuditAction? Action { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int PageSize { get; set; } = 50;
}
