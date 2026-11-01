using SecureKnowledgeBase.Domain.Entities;
using SecureKnowledgeBase.Domain.Enums;

namespace SecureKnowledgeBase.Domain.Interfaces;

public interface IAuditRepository
{
    Task AddAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditEvent>> GetRecentAsync(int count = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditEvent>> GetFilteredAsync(string? username = null, AuditAction? action = null, DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken cancellationToken = default);
}
