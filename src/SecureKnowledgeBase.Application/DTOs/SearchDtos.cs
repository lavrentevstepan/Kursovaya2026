using SecureKnowledgeBase.Domain.Enums;

namespace SecureKnowledgeBase.Application.DTOs;

public class SearchRequestDto
{
    public string Query { get; set; } = string.Empty;
    public string? Category { get; set; }
    public int MaxResults { get; set; } = 5;
}

public class SearchHitDto
{
    public Guid DocumentId { get; set; }
    public string DocumentTitle { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public AccessLevel AccessLevel { get; set; }
    public Guid ChunkId { get; set; }
    public int ChunkNumber { get; set; }
    public string Snippet { get; set; } = string.Empty;
    public string FullText { get; set; } = string.Empty;
    public string SectionHeader { get; set; } = string.Empty;
    public double Score { get; set; }
}

public class SearchResultDto
{
    public string Query { get; set; } = string.Empty;
    public int TotalHits { get; set; }
    public IReadOnlyList<SearchHitDto> Hits { get; set; } = Array.Empty<SearchHitDto>();
    public long ElapsedMilliseconds { get; set; }
}
