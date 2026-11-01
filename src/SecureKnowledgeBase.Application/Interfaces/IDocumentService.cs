using SecureKnowledgeBase.Application.DTOs;
using SecureKnowledgeBase.Domain.Enums;

namespace SecureKnowledgeBase.Application.Interfaces;

public interface IDocumentService
{
    Task<IReadOnlyList<DocumentDto>> GetDocumentsAsync(DocumentFilterDto filter, CancellationToken cancellationToken = default);
    Task<DocumentDto?> GetDocumentByIdAsync(Guid id, AccessLevel userAccessLevel, CancellationToken cancellationToken = default);
    Task<DocumentDto> UploadDocumentAsync(DocumentUploadRequestDto request, Guid ownerId, CancellationToken cancellationToken = default);
    Task<bool> DeleteDocumentAsync(Guid id, Guid currentUserId, UserRole currentUserRole, CancellationToken cancellationToken = default);
    Task<DocumentDto?> UpdateDocumentMetadataAsync(Guid id, DocumentUpdateDto dto, Guid currentUserId, UserRole currentUserRole, CancellationToken cancellationToken = default);
    Task<(Stream FileStream, string ContentType, string FileName)?> DownloadDocumentAsync(Guid id, AccessLevel userAccessLevel, CancellationToken cancellationToken = default);
}
