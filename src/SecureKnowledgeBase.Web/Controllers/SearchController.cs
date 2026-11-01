using Microsoft.AspNetCore.Mvc;
using SecureKnowledgeBase.Application.DTOs;
using SecureKnowledgeBase.Application.Interfaces;
using SecureKnowledgeBase.Domain.Enums;
using SecureKnowledgeBase.Domain.Interfaces;
using ISearchService = SecureKnowledgeBase.Application.Interfaces.ISearchService;

namespace SecureKnowledgeBase.Web.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDocumentIndexingService _indexingService;

    public SearchController(
        ISearchService searchService,
        ICurrentUserService currentUserService,
        IDocumentIndexingService indexingService)
    {
        _searchService = searchService;
        _currentUserService = currentUserService;
        _indexingService = indexingService;
    }

    [HttpGet]
    public async Task<ActionResult<SearchResultDto>> Search([FromQuery] string query, [FromQuery] string? category, [FromQuery] int maxResults = 10, CancellationToken cancellationToken = default)
    {
        var request = new SearchRequestDto
        {
            Query = query ?? string.Empty,
            Category = category,
            MaxResults = maxResults
        };

        var result = await _searchService.SearchAsync(request, _currentUserService.MaxAllowedAccessLevel, cancellationToken);
        return Ok(result);
    }

    [HttpPost("reindex")]
    public async Task<IActionResult> Reindex(CancellationToken cancellationToken)
    {
        var role = _currentUserService.Role ?? UserRole.SystemAdmin;
        if (role != UserRole.SystemAdmin && role != UserRole.KnowledgeAdmin)
        {
            return Forbid();
        }

        var totalChunks = await _indexingService.ReindexAllAsync(cancellationToken);
        return Ok(new
        {
            message = "Индексация базы знаний завершена",
            totalChunksIndexed = totalChunks
        });
    }
}
