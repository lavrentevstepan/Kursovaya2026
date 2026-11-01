using Microsoft.AspNetCore.Mvc;
using SecureKnowledgeBase.Application.DTOs;
using SecureKnowledgeBase.Application.Interfaces;
using SecureKnowledgeBase.Domain.Interfaces;

namespace SecureKnowledgeBase.Web.Controllers;

[ApiController]
[Route("api/v1/security-lab")]
public class SecurityLabController : ControllerBase
{
    private readonly IVulnerabilityLabService _labService;
    private readonly ICurrentUserService _currentUserService;

    public SecurityLabController(
        IVulnerabilityLabService labService,
        ICurrentUserService currentUserService)
    {
        _labService = labService;
        _currentUserService = currentUserService;
    }

    [HttpGet("status")]
    public async Task<ActionResult<VulnerabilityLabStatusDto>> GetStatus(CancellationToken cancellationToken)
    {
        var status = await _labService.GetStatusAsync(cancellationToken);
        return Ok(status);
    }

    [HttpPost("test-sqli")]
    public async Task<ActionResult<VulnerabilityDemoResultDto>> TestSqlInjection([FromBody] VulnerabilityTestRequestDto request, CancellationToken cancellationToken)
    {
        var username = _currentUserService.Username ?? "anonymous";
        var result = await _labService.TestSqlInjectionAsync(request.Payload, username, cancellationToken);
        return Ok(result);
    }

    [HttpPost("test-traversal")]
    public async Task<ActionResult<VulnerabilityDemoResultDto>> TestPathTraversal([FromBody] VulnerabilityTestRequestDto request, CancellationToken cancellationToken)
    {
        var username = _currentUserService.Username ?? "anonymous";
        var result = await _labService.TestPathTraversalAsync(request.Payload, username, cancellationToken);
        return Ok(result);
    }

    [HttpPost("test-file-upload")]
    public async Task<ActionResult<VulnerabilityDemoResultDto>> TestFileUpload([FromBody] VulnerabilityTestRequestDto request, CancellationToken cancellationToken)
    {
        var username = _currentUserService.Username ?? "anonymous";
        var fakeBytes = System.Text.Encoding.UTF8.GetBytes("MZ_SIMULATED_TEST_EXECUTABLE_PAYLOAD");
        var result = await _labService.TestFileUploadBypassAsync(request.Payload, fakeBytes, username, cancellationToken);
        return Ok(result);
    }

    [HttpPost("test-prompt-injection")]
    public async Task<ActionResult<VulnerabilityDemoResultDto>> TestPromptInjection([FromBody] VulnerabilityTestRequestDto request, CancellationToken cancellationToken)
    {
        var username = _currentUserService.Username ?? "anonymous";
        var result = await _labService.TestPromptInjectionAsync(request.Payload, username, cancellationToken);
        return Ok(result);
    }
}
