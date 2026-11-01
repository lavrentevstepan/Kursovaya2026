using Microsoft.AspNetCore.Mvc;
using SecureKnowledgeBase.Application.DTOs;
using SecureKnowledgeBase.Application.Interfaces;

namespace SecureKnowledgeBase.Web.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;

    public AuthController(IAuthenticationService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResultDto>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.AuthenticateAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    [HttpGet("demo-users")]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetDemoUsers(CancellationToken cancellationToken)
    {
        var users = await _authService.GetDemoUsersAsync(cancellationToken);
        return Ok(users);
    }
}
