using SecureKnowledgeBase.Domain.Enums;

namespace SecureKnowledgeBase.Application.DTOs;

public class AssistantQueryRequestDto
{
    public Guid? SessionId { get; set; }
    public string Query { get; set; } = string.Empty;
    public ChatResponseMode Mode { get; set; } = ChatResponseMode.Extractive;
}

public class SourceCitationDto
{
    public int CitationIndex { get; set; }
    public Guid DocumentId { get; set; }
    public string DocumentTitle { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int ChunkNumber { get; set; }
    public string SectionHeader { get; set; } = string.Empty;
    public string TextSnippet { get; set; } = string.Empty;
    public double RelevanceScore { get; set; }
}

public class AssistantResponseDto
{
    public Guid SessionId { get; set; }
    public string Query { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public ChatResponseMode ResponseModeUsed { get; set; }
    public IReadOnlyList<SourceCitationDto> Sources { get; set; } = Array.Empty<SourceCitationDto>();
    public long ElapsedMilliseconds { get; set; }
    public bool FallbackToExtractiveOccurred { get; set; }
}
