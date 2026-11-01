namespace SecureKnowledgeBase.Domain.Entities;

public class DocumentChunk
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }
    public int ChunkNumber { get; set; }
    public string TextContent { get; set; } = string.Empty;
    public string SectionHeader { get; set; } = string.Empty;
    public bool IsIndexed { get; set; } = false;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
