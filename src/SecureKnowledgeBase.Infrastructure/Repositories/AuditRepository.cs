using Microsoft.EntityFrameworkCore;
using SecureKnowledgeBase.Domain.Entities;
using SecureKnowledgeBase.Domain.Enums;
using SecureKnowledgeBase.Domain.Interfaces;
using SecureKnowledgeBase.Infrastructure.Persistence;

namespace SecureKnowledgeBase.Infrastructure.Repositories;

public class AuditRepository : IAuditRepository
{
    private readonly AppDbContext _context;

    public AuditRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        await _context.AuditEvents.AddAsync(auditEvent, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEvent>> GetRecentAsync(int count = 50, CancellationToken cancellationToken = default)
    {
        return await _context.AuditEvents
            .OrderByDescending(e => e.TimestampUtc)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEvent>> GetFilteredAsync(string? username = null, AuditAction? action = null, DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken cancellationToken = default)
    {
        var query = _context.AuditEvents.AsQueryable();

        if (!string.IsNullOrWhiteSpace(username))
        {
            query = query.Where(e => e.Username.ToLower().Contains(username.ToLower()));
        }

        if (action.HasValue)
        {
            query = query.Where(e => e.Action == action.Value);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(e => e.TimestampUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(e => e.TimestampUtc <= toUtc.Value);
        }

        return await query.OrderByDescending(e => e.TimestampUtc).ToListAsync(cancellationToken);
    }
}
