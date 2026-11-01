using Microsoft.Extensions.Options;
using SecureKnowledgeBase.Application.DTOs;
using SecureKnowledgeBase.Application.Interfaces;
using SecureKnowledgeBase.Domain.Entities;
using SecureKnowledgeBase.Domain.Enums;
using SecureKnowledgeBase.Domain.Interfaces;
using SecureKnowledgeBase.Infrastructure.Configuration;

namespace SecureKnowledgeBase.Infrastructure.Services;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IAuditService _auditService;
    private readonly IUserRepository _userRepository;
    private readonly IDocumentIndexingService _indexingService;
    private readonly SecurityOptions _options;

    public DocumentService(
        IDocumentRepository documentRepository,
        IFileStorageService fileStorageService,
        IAuditService auditService,
        IUserRepository userRepository,
        IDocumentIndexingService indexingService,
        IOptions<SecurityOptions> options)
    {
        _documentRepository = documentRepository;
        _fileStorageService = fileStorageService;
        _auditService = auditService;
        _userRepository = userRepository;
        _indexingService = indexingService;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<DocumentDto>> GetDocumentsAsync(DocumentFilterDto filter, CancellationToken cancellationToken = default)
    {
        var documents = await _documentRepository.GetAllAsync(filter.MaxAccessLevel, cancellationToken);
        var query = documents.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter.Category))
        {
            query = query.Where(d => string.Equals(d.Category, filter.Category, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(d => d.Status == filter.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLowerInvariant();
            query = query.Where(d => d.Title.ToLowerInvariant().Contains(term) || d.Description.ToLowerInvariant().Contains(term));
        }

        if (filter.OwnerId.HasValue)
        {
            query = query.Where(d => d.OwnerId == filter.OwnerId.Value);
        }

        return query.Select(MapToDto).ToList();
    }

    public async Task<DocumentDto?> GetDocumentByIdAsync(Guid id, AccessLevel userAccessLevel, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(id, cancellationToken);
        if (document == null || document.Status == DocumentStatus.Deleted)
        {
            return null;
        }

        if ((int)document.AccessLevel > (int)userAccessLevel)
        {
            await _auditService.LogAsync("user", AuditAction.AccessDenied, "Document", id.ToString(), "Denied", "{\"reason\":\"insufficient_access_level\"}", cancellationToken: cancellationToken);
            return null;
        }

        return MapToDto(document);
    }

    public async Task<DocumentDto> UploadDocumentAsync(DocumentUploadRequestDto request, Guid ownerId, CancellationToken cancellationToken = default)
    {
        if (request.FileStream == null || request.FileLength <= 0)
        {
            throw new ArgumentException("Файл пуст или отсутствует");
        }

        var extension = Path.GetExtension(request.OriginalFileName).ToLowerInvariant();
        if (!_options.AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException($"Недопустимый формат файла. Разрешены: {string.Join(", ", _options.AllowedExtensions)}");
        }

        var maxBytes = (long)_options.MaxFileSizeMb * 1024 * 1024;
        if (request.FileLength > maxBytes)
        {
            throw new ArgumentException($"Размер файла ({request.FileLength / 1024 / 1024} МБ) превышает лимит ({_options.MaxFileSizeMb} МБ)");
        }

        var owner = await _userRepository.GetByIdAsync(ownerId, cancellationToken);
        var ownerUsername = owner?.Username ?? "unknown";

        var (storedFileName, sha256Hash, sizeBytes) = await _fileStorageService.SaveFileAsync(request.FileStream, request.OriginalFileName, cancellationToken);

        var document = new Document
        {
            Id = Guid.NewGuid(),
            Title = string.IsNullOrWhiteSpace(request.Title) ? Path.GetFileNameWithoutExtension(request.OriginalFileName) : request.Title.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Category = string.IsNullOrWhiteSpace(request.Category) ? "Общие" : request.Category.Trim(),
            AccessLevel = request.AccessLevel,
            Status = DocumentStatus.Uploaded,
            OwnerId = ownerId,
            CreatedAtUtc = DateTime.UtcNow,
            OriginalFileName = Path.GetFileName(request.OriginalFileName),
            StoredFileName = storedFileName,
            ContentType = string.IsNullOrWhiteSpace(request.ContentType) ? "application/octet-stream" : request.ContentType,
            FileSizeBytes = sizeBytes,
            FileSha256Hash = sha256Hash
        };

        var job = new ProcessingJob
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            JobType = "IngestAndIndex",
            Status = ProcessingJobStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
        document.ProcessingJobs.Add(job);

        await _documentRepository.AddAsync(document, cancellationToken);
        await _indexingService.IndexDocumentAsync(document.Id, cancellationToken);

        await _auditService.LogAsync(
            ownerUsername,
            AuditAction.DocumentUpload,
            "Document",
            document.Id.ToString(),
            "Success",
            $"{{\"fileName\":\"{document.OriginalFileName}\",\"size\":{sizeBytes},\"hash\":\"{sha256Hash}\",\"access\":\"{document.AccessLevel}\"}}",
            cancellationToken: cancellationToken);

        var freshDoc = await _documentRepository.GetByIdAsync(document.Id, cancellationToken);
        return MapToDto(freshDoc ?? document);
    }


    public async Task<bool> DeleteDocumentAsync(Guid id, Guid currentUserId, UserRole currentUserRole, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(id, cancellationToken);
        if (document == null || document.Status == DocumentStatus.Deleted)
        {
            return false;
        }

        var canDelete = currentUserRole == UserRole.SystemAdmin || currentUserRole == UserRole.KnowledgeAdmin || document.OwnerId == currentUserId;
        if (!canDelete)
        {
            await _auditService.LogAsync("user", AuditAction.AccessDenied, "Document", id.ToString(), "Denied", "{\"reason\":\"delete_not_permitted\"}", cancellationToken: cancellationToken);
            return false;
        }

        await _documentRepository.DeleteAsync(id, cancellationToken);
        await _fileStorageService.DeleteFileAsync(document.StoredFileName, cancellationToken);

        await _auditService.LogAsync(
            currentUserRole.ToString(),
            AuditAction.DocumentDelete,
            "Document",
            id.ToString(),
            "Success",
            $"{{\"title\":\"{document.Title}\"}}",
            cancellationToken: cancellationToken);

        return true;
    }

    public async Task<DocumentDto?> UpdateDocumentMetadataAsync(Guid id, DocumentUpdateDto dto, Guid currentUserId, UserRole currentUserRole, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(id, cancellationToken);
        if (document == null || document.Status == DocumentStatus.Deleted)
        {
            return null;
        }

        var canEdit = currentUserRole == UserRole.SystemAdmin || currentUserRole == UserRole.KnowledgeAdmin || document.OwnerId == currentUserId;
        if (!canEdit)
        {
            await _auditService.LogAsync(currentUserRole.ToString(), AuditAction.AccessDenied, "Document", id.ToString(), "Denied", "{\"reason\":\"edit_not_permitted\"}", cancellationToken: cancellationToken);
            return null;
        }

        document.Title = string.IsNullOrWhiteSpace(dto.Title) ? document.Title : dto.Title.Trim();
        document.Description = dto.Description?.Trim() ?? string.Empty;
        document.Category = string.IsNullOrWhiteSpace(dto.Category) ? document.Category : dto.Category.Trim();
        document.AccessLevel = dto.AccessLevel;
        document.UpdatedAtUtc = DateTime.UtcNow;

        await _documentRepository.UpdateAsync(document, cancellationToken);

        await _auditService.LogAsync(
            currentUserRole.ToString(),
            AuditAction.DocumentUpdate,
            "Document",
            id.ToString(),
            "Success",
            $"{{\"title\":\"{document.Title}\",\"category\":\"{document.Category}\",\"access\":\"{document.AccessLevel}\"}}",
            cancellationToken: cancellationToken);

        return MapToDto(document);
    }


    public async Task<(Stream FileStream, string ContentType, string FileName)?> DownloadDocumentAsync(Guid id, AccessLevel userAccessLevel, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(id, cancellationToken);
        if (document == null || document.Status == DocumentStatus.Deleted)
        {
            return null;
        }

        if ((int)document.AccessLevel > (int)userAccessLevel)
        {
            await _auditService.LogAsync("user", AuditAction.AccessDenied, "Document", id.ToString(), "Denied", "{\"reason\":\"download_insufficient_access\"}", cancellationToken: cancellationToken);
            return null;
        }

        var stream = await _fileStorageService.GetFileStreamAsync(document.StoredFileName, cancellationToken);
        if (stream == null)
        {
            return null;
        }

        await _auditService.LogAsync("user", AuditAction.FileDownload, "Document", id.ToString(), "Success", $"{{\"fileName\":\"{document.OriginalFileName}\"}}", cancellationToken: cancellationToken);
        return (stream, document.ContentType, document.OriginalFileName);
    }

    private static DocumentDto MapToDto(Document doc)
    {
        return new DocumentDto
        {
            Id = doc.Id,
            Title = doc.Title,
            Description = doc.Description,
            Category = doc.Category,
            AccessLevel = doc.AccessLevel,
            Status = doc.Status,
            OwnerId = doc.OwnerId,
            OwnerUsername = doc.Owner?.Username ?? "admin",
            CreatedAtUtc = doc.CreatedAtUtc,
            UpdatedAtUtc = doc.UpdatedAtUtc,
            OriginalFileName = doc.OriginalFileName,
            ContentType = doc.ContentType,
            FileSizeBytes = doc.FileSizeBytes,
            FileSha256Hash = doc.FileSha256Hash,
            ChunkCount = doc.Chunks.Count
        };
    }
}
