using SecureKnowledgeBase.Application.DTOs;

namespace SecureKnowledgeBase.Application.Interfaces;

public interface ILocalLlmClient
{
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    Task<string> GenerateAnswerAsync(string question, IReadOnlyList<RagSourceItemDto> contextSources, CancellationToken cancellationToken = default);
    Task<LlmStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);
}
