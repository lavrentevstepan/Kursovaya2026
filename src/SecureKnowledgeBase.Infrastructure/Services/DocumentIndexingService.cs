using SecureKnowledgeBase.Application.Interfaces;
using SecureKnowledgeBase.Domain.Entities;
using SecureKnowledgeBase.Domain.Enums;
using SecureKnowledgeBase.Domain.Interfaces;

namespace SecureKnowledgeBase.Infrastructure.Services;

public class DocumentIndexingService : IDocumentIndexingService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ITextExtractor _textExtractor;
    private readonly ITextChunker _textChunker;
    private readonly IAuditService _auditService;

    public DocumentIndexingService(
        IDocumentRepository documentRepository,
        IFileStorageService fileStorageService,
        ITextExtractor textExtractor,
        ITextChunker textChunker,
        IAuditService auditService)
    {
        _documentRepository = documentRepository;
        _fileStorageService = fileStorageService;
        _textExtractor = textExtractor;
        _textChunker = textChunker;
        _auditService = auditService;
    }

    public async Task<int> IndexDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken);
        if (document == null || document.Status == DocumentStatus.Deleted)
        {
            return 0;
        }

        document.Status = DocumentStatus.Processing;
        await _documentRepository.UpdateAsync(document, cancellationToken);

        try
        {
            await using var stream = await _fileStorageService.GetFileStreamAsync(document.StoredFileName, cancellationToken);
            if (stream == null)
            {
                document.Status = DocumentStatus.Failed;
                await _documentRepository.UpdateAsync(document, cancellationToken);
                return 0;
            }

            var extractedText = await _textExtractor.ExtractTextAsync(stream, document.OriginalFileName, cancellationToken);

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                extractedText = $"{document.Title}\n{document.Description}";
            }

            var chunkTuples = _textChunker.ChunkText(extractedText);
            var chunks = chunkTuples.Select(c => new DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                ChunkNumber = c.ChunkNumber,
                TextContent = c.Text,
                SectionHeader = c.SectionHeader,
                IsIndexed = true,
                CreatedAtUtc = DateTime.UtcNow
            }).ToList();

            if (chunks.Count == 0)
            {
                chunks.Add(new DocumentChunk
                {
                    Id = Guid.NewGuid(),
                    DocumentId = document.Id,
                    ChunkNumber = 1,
                    TextContent = $"{document.Title}. {document.Description}",
                    SectionHeader = "Основная часть",
                    IsIndexed = true,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            await _documentRepository.DeleteChunksByDocumentIdAsync(document.Id, cancellationToken);
            await _documentRepository.AddChunksAsync(chunks, cancellationToken);

            document.Status = DocumentStatus.Indexed;
            document.UpdatedAtUtc = DateTime.UtcNow;

            foreach (var job in document.ProcessingJobs.Where(j => j.Status == ProcessingJobStatus.Pending || j.Status == ProcessingJobStatus.InProgress))
            {
                job.Status = ProcessingJobStatus.Completed;
                job.CompletedAtUtc = DateTime.UtcNow;
            }

            await _documentRepository.UpdateAsync(document, cancellationToken);

            await _auditService.LogAsync(
                "system",
                AuditAction.SecurityEvent,
                "Document",
                document.Id.ToString(),
                "Success",
                $"{{\"action\":\"IndexingCompleted\",\"chunkCount\":{chunks.Count}}}",
                cancellationToken: cancellationToken);

            return chunks.Count;
        }
        catch (Exception ex)
        {
            document.Status = DocumentStatus.Failed;
            foreach (var job in document.ProcessingJobs.Where(j => j.Status == ProcessingJobStatus.Pending || j.Status == ProcessingJobStatus.InProgress))
            {
                job.Status = ProcessingJobStatus.Failed;
                job.ErrorMessage = ex.Message;
                job.CompletedAtUtc = DateTime.UtcNow;
            }

            await _documentRepository.UpdateAsync(document, cancellationToken);

            await _auditService.LogAsync(
                "system",
                AuditAction.SecurityEvent,
                "Document",
                document.Id.ToString(),
                "Failed",
                $"{{\"action\":\"IndexingFailed\",\"error\":\"{ex.Message}\"}}",
                cancellationToken: cancellationToken);

            return 0;
        }
    }

    public async Task<int> ReindexAllAsync(CancellationToken cancellationToken = default)
    {
        var documents = await _documentRepository.GetAllAsync(null, cancellationToken);
        int totalIndexedChunks = 0;

        foreach (var doc in documents.Where(d => d.Status != DocumentStatus.Deleted))
        {
            var count = await IndexDocumentAsync(doc.Id, cancellationToken);
            totalIndexedChunks += count;
        }

        return totalIndexedChunks;
    }
}
