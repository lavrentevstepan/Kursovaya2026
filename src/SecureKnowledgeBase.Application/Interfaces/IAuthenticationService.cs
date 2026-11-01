using SecureKnowledgeBase.Application.DTOs;

namespace SecureKnowledgeBase.Application.Interfaces;

public interface IAuthenticationService
{
    Task<LoginResultDto> AuthenticateAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserDto>> GetDemoUsersAsync(CancellationToken cancellationToken = default);
}
