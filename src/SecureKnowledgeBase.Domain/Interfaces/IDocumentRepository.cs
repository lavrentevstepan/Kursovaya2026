using SecureKnowledgeBase.Domain.Entities;
using SecureKnowledgeBase.Domain.Enums;

namespace SecureKnowledgeBase.Domain.Interfaces;

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Document>> GetAllAsync(AccessLevel? maxAccessLevel = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Document>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task AddAsync(Document document, CancellationToken cancellationToken = default);
    Task UpdateAsync(Document document, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentChunk>> GetChunksByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task AddChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default);
    Task DeleteChunksByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);
}
