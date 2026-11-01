using SecureKnowledgeBase.Application.DTOs;
using SecureKnowledgeBase.Domain.Enums;

namespace SecureKnowledgeBase.Application.Interfaces;

public interface ISearchService
{
    Task<SearchResultDto> SearchAsync(SearchRequestDto request, AccessLevel maxAccessLevel, CancellationToken cancellationToken = default);
}
