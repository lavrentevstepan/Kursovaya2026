using SecureKnowledgeBase.Application.DTOs;
using SecureKnowledgeBase.Domain.Enums;

namespace SecureKnowledgeBase.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(string username, AuditAction action, string targetType, string targetId, string result, string metadataJson = "", string clientIp = "", CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditEventDto>> GetEventsAsync(AuditFilterDto filter, CancellationToken cancellationToken = default);
}
