using Microsoft.AspNetCore.Mvc;
using SecureKnowledgeBase.Application.Interfaces;

namespace SecureKnowledgeBase.Web.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly ISystemStatusService _statusService;

    public HealthController(ISystemStatusService statusService)
    {
        _statusService = statusService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var isHealthy = await _statusService.IsHealthyAsync(cancellationToken);
        var status = await _statusService.GetStatusAsync(cancellationToken);

        if (isHealthy)
        {
            return Ok(new
            {
                status = "Healthy",
                application = status.ApplicationName,
                version = status.Version,
                environment = status.Environment,
                uptimeSeconds = status.UptimeSeconds,
                timestamp = status.TimestampUtc,
                services = status.Services
            });
        }

        return StatusCode(503, new
        {
            status = "Unhealthy",
            timestamp = DateTime.UtcNow
        });
    }
}
