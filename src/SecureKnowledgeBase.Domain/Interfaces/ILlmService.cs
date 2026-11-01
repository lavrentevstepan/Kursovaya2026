namespace SecureKnowledgeBase.Domain.Interfaces;

public class LlmModelInfo
{
    public string ModelName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public string Provider { get; set; } = string.Empty;
}

public interface ILlmService
{
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    Task<LlmModelInfo> GetModelInfoAsync(CancellationToken cancellationToken = default);
    Task<string> GenerateAnswerAsync(string prompt, IReadOnlyList<string> contextChunks, CancellationToken cancellationToken = default);
}
