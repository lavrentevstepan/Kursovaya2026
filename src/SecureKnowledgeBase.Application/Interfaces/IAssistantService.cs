using SecureKnowledgeBase.Application.DTOs;
using SecureKnowledgeBase.Domain.Enums;

namespace SecureKnowledgeBase.Application.Interfaces;

public interface IAssistantService
{
    Task<AssistantResponseDto> AskAsync(AssistantQueryRequestDto request, Guid userId, AccessLevel userAccessLevel, CancellationToken cancellationToken = default);
    Task<SearchResultDto> SearchAsync(SearchRequestDto request, AccessLevel userAccessLevel, CancellationToken cancellationToken = default);
}
