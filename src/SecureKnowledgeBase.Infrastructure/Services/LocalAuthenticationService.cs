using System.Security.Cryptography;
using System.Text;
using SecureKnowledgeBase.Application.DTOs;
using SecureKnowledgeBase.Application.Interfaces;
using SecureKnowledgeBase.Domain.Enums;
using SecureKnowledgeBase.Domain.Interfaces;

namespace SecureKnowledgeBase.Infrastructure.Services;

public class LocalAuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;

    public LocalAuthenticationService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<LoginResultDto> AuthenticateAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return LoginResultDto.Failure("Укажите имя пользователя и пароль");
        }

        var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (user == null)
        {
            return LoginResultDto.Failure("Пользователь не найден");
        }

        if (!user.IsActive)
        {
            return LoginResultDto.Failure("Учетная запись деактивирована");
        }

        var providedHash = HashPassword(request.Password);
        if (user.PasswordHash != providedHash)
        {
            return LoginResultDto.Failure("Неверный пароль");
        }

        user.LastLoginAtUtc = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, cancellationToken);

        var maxAccess = user.Role switch
        {
            UserRole.SystemAdmin => AccessLevel.Confidential,
            UserRole.SecurityAuditor => AccessLevel.Confidential,
            UserRole.KnowledgeAdmin => AccessLevel.Restricted,
            _ => AccessLevel.PublicInternal
        };

        var userDto = new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Role = user.Role,
            MaxAccessLevel = maxAccess,
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc,
            LastLoginAtUtc = user.LastLoginAtUtc
        };

        var simulatedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Id}:{user.Username}:{user.Role}:{DateTime.UtcNow.Ticks}"));
        return LoginResultDto.Success(userDto, simulatedToken);
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null)
        {
            return null;
        }

        return MapToDto(user);
    }

    public async Task<UserDto?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByUsernameAsync(username, cancellationToken);
        if (user == null)
        {
            return null;
        }

        return MapToDto(user);
    }

    public async Task<IReadOnlyList<UserDto>> GetDemoUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        return users.Select(MapToDto).ToList();
    }

    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password + "_SKB_SALT_2026"));
        return Convert.ToHexString(bytes);
    }

    private static UserDto MapToDto(Domain.Entities.User user)
    {
        var maxAccess = user.Role switch
        {
            UserRole.SystemAdmin => AccessLevel.Confidential,
            UserRole.SecurityAuditor => AccessLevel.Confidential,
            UserRole.KnowledgeAdmin => AccessLevel.Restricted,
            _ => AccessLevel.PublicInternal
        };

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Role = user.Role,
            MaxAccessLevel = maxAccess,
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc,
            LastLoginAtUtc = user.LastLoginAtUtc
        };
    }
}
