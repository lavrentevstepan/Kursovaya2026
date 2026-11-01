using SecureKnowledgeBase.Application.DTOs;

namespace SecureKnowledgeBase.Application.Interfaces;

public interface ISystemStatusService
{
    Task<SystemStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
