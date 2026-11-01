using SecureKnowledgeBase.Application.DTOs;
using SecureKnowledgeBase.Application.Interfaces;
using SecureKnowledgeBase.Domain.Entities;
using SecureKnowledgeBase.Domain.Enums;
using SecureKnowledgeBase.Domain.Interfaces;

namespace SecureKnowledgeBase.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IAuditRepository _auditRepository;

    public AuditService(IAuditRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }

    public async Task LogAsync(string username, AuditAction action, string targetType, string targetId, string result, string metadataJson = "", string clientIp = "", CancellationToken cancellationToken = default)
    {
        var sanitizedMetadata = SanitizeMetadata(metadataJson);

        var auditEvent = new AuditEvent
        {
            Id = Guid.NewGuid(),
            TimestampUtc = DateTime.UtcNow,
            Username = string.IsNullOrWhiteSpace(username) ? "anonymous" : username,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Result = result,
            MetadataJson = sanitizedMetadata,
            ClientIpAddress = string.IsNullOrWhiteSpace(clientIp) ? "127.0.0.1" : clientIp
        };

        await _auditRepository.AddAsync(auditEvent, cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEventDto>> GetEventsAsync(AuditFilterDto filter, CancellationToken cancellationToken = default)
    {
        var events = await _auditRepository.GetFilteredAsync(filter.Username, filter.Action, filter.FromUtc, filter.ToUtc, cancellationToken);

        return events.Take(filter.PageSize).Select(e => new AuditEventDto
        {
            Id = e.Id,
            TimestampUtc = e.TimestampUtc,
            Username = e.Username,
            Action = e.Action,
            TargetType = e.TargetType,
            TargetId = e.TargetId,
            Result = e.Result,
            MetadataJson = e.MetadataJson,
            ClientIpAddress = e.ClientIpAddress
        }).ToList();
    }

    private static string SanitizeMetadata(string metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata)) return string.Empty;

        var lower = metadata.ToLowerInvariant();
        if (lower.Contains("password") || lower.Contains("secret") || lower.Contains("token"))
        {
            return "{\"redacted\":\"Sensitive information was removed\"}";
        }

        return metadata;
    }
}
