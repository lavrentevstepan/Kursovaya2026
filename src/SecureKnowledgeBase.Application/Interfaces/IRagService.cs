using SecureKnowledgeBase.Application.DTOs;
using SecureKnowledgeBase.Domain.Enums;

namespace SecureKnowledgeBase.Application.Interfaces;

public interface IRagService
{
    Task<RagQueryResponseDto> AskAsync(RagQueryRequestDto request, Guid userId, string username, AccessLevel userAccessLevel, CancellationToken cancellationToken = default);
    Task<LlmStatusDto> GetLlmStatusAsync(CancellationToken cancellationToken = default);
}
