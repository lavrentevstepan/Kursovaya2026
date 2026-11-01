using SecureKnowledgeBase.Domain.Entities;
using SecureKnowledgeBase.Domain.Enums;

namespace SecureKnowledgeBase.Domain.Interfaces;

public class SearchResultItem
{
    public Guid DocumentId { get; set; }
    public string DocumentTitle { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public AccessLevel AccessLevel { get; set; }
    public Guid ChunkId { get; set; }
    public int ChunkNumber { get; set; }
    public string ChunkText { get; set; } = string.Empty;
    public string SectionHeader { get; set; } = string.Empty;
    public double Score { get; set; }
}

public interface ISearchService
{
    Task<IReadOnlyList<SearchResultItem>> SearchAsync(string query, AccessLevel userAccessLevel, int maxResults = 5, CancellationToken cancellationToken = default);
    Task IndexChunkAsync(DocumentChunk chunk, Document document, CancellationToken cancellationToken = default);
    Task RemoveDocumentIndexAsync(Guid documentId, CancellationToken cancellationToken = default);
}
