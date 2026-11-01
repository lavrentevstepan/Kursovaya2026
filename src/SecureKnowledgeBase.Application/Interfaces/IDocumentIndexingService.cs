namespace SecureKnowledgeBase.Application.Interfaces;

public interface IDocumentIndexingService
{
    Task<int> IndexDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<int> ReindexAllAsync(CancellationToken cancellationToken = default);
}
