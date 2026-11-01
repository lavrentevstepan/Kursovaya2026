using SecureKnowledgeBase.Domain.Enums;

namespace SecureKnowledgeBase.Application.DTOs;

public enum RagMode
{
    Auto = 1,
    ExtractiveOnly = 2,
    LlmOnly = 3
}

public class RagQueryRequestDto
{
    public string Question { get; set; } = string.Empty;
    public RagMode Mode { get; set; } = RagMode.Auto;
    public string? Category { get; set; }
    public int MaxSources { get; set; } = 3;
}

public class RagSourceItemDto
{
    public Guid ChunkId { get; set; }
    public Guid DocumentId { get; set; }
    public string DocumentTitle { get; set; } = string.Empty;
    public string SectionHeader { get; set; } = string.Empty;
    public AccessLevel AccessLevel { get; set; }
    public double RelevanceScore { get; set; }
    public string Snippet { get; set; } = string.Empty;
    public string FullText { get; set; } = string.Empty;
}

public class RagQueryResponseDto
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public RagMode ModeUsed { get; set; }
    public bool HasFoundAnswer { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public List<RagSourceItemDto> Sources { get; set; } = new();
}

public class LlmStatusDto
{
    public bool IsEnabledInConfig { get; set; }
    public bool IsEndpointReachable { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string StatusMessage { get; set; } = string.Empty;
}
