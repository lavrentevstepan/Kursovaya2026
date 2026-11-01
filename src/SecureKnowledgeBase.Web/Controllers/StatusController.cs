using Microsoft.AspNetCore.Mvc;
using SecureKnowledgeBase.Application.DTOs;
using SecureKnowledgeBase.Application.Interfaces;

namespace SecureKnowledgeBase.Web.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class StatusController : ControllerBase
{
    private readonly ISystemStatusService _statusService;

    public StatusController(ISystemStatusService statusService)
    {
        _statusService = statusService;
    }

    [HttpGet]
    public async Task<ActionResult<SystemStatusDto>> Get(CancellationToken cancellationToken)
    {
        var status = await _statusService.GetStatusAsync(cancellationToken);
        return Ok(status);
    }
}
