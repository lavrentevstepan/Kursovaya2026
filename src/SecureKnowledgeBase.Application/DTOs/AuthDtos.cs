using SecureKnowledgeBase.Domain.Enums;

namespace SecureKnowledgeBase.Application.DTOs;

public class LoginRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public AccessLevel MaxAccessLevel { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
}

public class LoginResultDto
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public UserDto? User { get; set; }
    public string? Token { get; set; }

    public static LoginResultDto Success(UserDto user, string token) => new()
    {
        IsSuccess = true,
        User = user,
        Token = token
    };

    public static LoginResultDto Failure(string error) => new()
    {
        IsSuccess = false,
        ErrorMessage = error
    };
}
