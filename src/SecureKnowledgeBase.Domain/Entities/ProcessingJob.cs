using SecureKnowledgeBase.Domain.Enums;

namespace SecureKnowledgeBase.Domain.Entities;

public class ProcessingJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }
    public string JobType { get; set; } = string.Empty;
    public ProcessingJobStatus Status { get; set; } = ProcessingJobStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
}
