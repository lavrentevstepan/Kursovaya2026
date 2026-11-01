using SecureKnowledgeBase.Domain.Enums;

namespace SecureKnowledgeBase.Application.DTOs;

public class DocumentDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public AccessLevel AccessLevel { get; set; }
    public DocumentStatus Status { get; set; }
    public Guid OwnerId { get; set; }
    public string OwnerUsername { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string FileSha256Hash { get; set; } = string.Empty;
    public int ChunkCount { get; set; }
}

public class DocumentUploadRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public AccessLevel AccessLevel { get; set; } = AccessLevel.PublicInternal;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public Stream FileStream { get; set; } = Stream.Null;
    public long FileLength { get; set; }
}

public class DocumentFilterDto
{
    public string? SearchTerm { get; set; }
    public string? Category { get; set; }
    public AccessLevel? MaxAccessLevel { get; set; }
    public DocumentStatus? Status { get; set; }
    public Guid? OwnerId { get; set; }
}

public class DocumentUpdateDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public AccessLevel AccessLevel { get; set; }
}

