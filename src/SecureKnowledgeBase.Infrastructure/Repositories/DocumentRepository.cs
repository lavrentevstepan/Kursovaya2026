using Microsoft.EntityFrameworkCore;
using SecureKnowledgeBase.Domain.Entities;
using SecureKnowledgeBase.Domain.Enums;
using SecureKnowledgeBase.Domain.Interfaces;
using SecureKnowledgeBase.Infrastructure.Persistence;

namespace SecureKnowledgeBase.Infrastructure.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly AppDbContext _context;

    public DocumentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Documents
            .Include(d => d.Owner)
            .Include(d => d.Chunks)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> GetAllAsync(AccessLevel? maxAccessLevel = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Documents
            .Include(d => d.Owner)
            .Where(d => d.Status != DocumentStatus.Deleted);

        if (maxAccessLevel.HasValue)
        {
            query = query.Where(d => (int)d.AccessLevel <= (int)maxAccessLevel.Value);
        }

        return await query.OrderByDescending(d => d.CreatedAtUtc).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await _context.Documents
            .Where(d => d.OwnerId == ownerId && d.Status != DocumentStatus.Deleted)
            .OrderByDescending(d => d.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        await _context.Documents.AddAsync(document, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Document document, CancellationToken cancellationToken = default)
    {
        document.UpdatedAtUtc = DateTime.UtcNow;
        _context.Documents.Update(document);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var doc = await _context.Documents.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (doc != null)
        {
            doc.Status = DocumentStatus.Deleted;
            doc.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<DocumentChunk>> GetChunksByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        return await _context.DocumentChunks
            .Where(c => c.DocumentId == documentId)
            .OrderBy(c => c.ChunkNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task AddChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        await _context.DocumentChunks.AddRangeAsync(chunks, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteChunksByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var chunks = await _context.DocumentChunks.Where(c => c.DocumentId == documentId).ToListAsync(cancellationToken);
        if (chunks.Count > 0)
        {
            _context.DocumentChunks.RemoveRange(chunks);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
