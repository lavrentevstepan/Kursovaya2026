using Microsoft.AspNetCore.Mvc;
using SecureKnowledgeBase.Application.DTOs;
using SecureKnowledgeBase.Application.Interfaces;
using SecureKnowledgeBase.Domain.Interfaces;

namespace SecureKnowledgeBase.Web.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AssistantController : ControllerBase
{
    private readonly IRagService _ragService;
    private readonly ICurrentUserService _currentUserService;

    public AssistantController(IRagService ragService, ICurrentUserService currentUserService)
    {
        _ragService = ragService;
        _currentUserService = currentUserService;
    }

    [HttpPost("ask")]
    public async Task<ActionResult<RagQueryResponseDto>> Ask([FromBody] RagQueryRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest(new { error = "Параметр 'question' обязателен для заполнения." });
        }

        var userId = _currentUserService.UserId ?? Guid.Empty;
        var username = _currentUserService.Username ?? "anonymous";
        var accessLevel = _currentUserService.MaxAllowedAccessLevel;

        var result = await _ragService.AskAsync(request, userId, username, accessLevel, cancellationToken);
        return Ok(result);
    }

    [HttpGet("status")]
    public async Task<ActionResult<LlmStatusDto>> GetStatus(CancellationToken cancellationToken = default)
    {
        var status = await _ragService.GetLlmStatusAsync(cancellationToken);
        return Ok(status);
    }
}
