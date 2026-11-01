using SecureKnowledgeBase.Domain.Enums;

namespace SecureKnowledgeBase.Domain.Entities;

public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public AccessLevel AccessLevel { get; set; } = AccessLevel.PublicInternal;
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public Guid OwnerId { get; set; }
    public User? Owner { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string FileSha256Hash { get; set; } = string.Empty;

    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
    public ICollection<ProcessingJob> ProcessingJobs { get; set; } = new List<ProcessingJob>();
}
