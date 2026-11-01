using Microsoft.AspNetCore.Mvc;
using SecureKnowledgeBase.Application.DTOs;
using SecureKnowledgeBase.Application.Interfaces;
using SecureKnowledgeBase.Domain.Enums;
using SecureKnowledgeBase.Domain.Interfaces;

namespace SecureKnowledgeBase.Web.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ICurrentUserService _currentUserService;

    public DocumentsController(IDocumentService documentService, ICurrentUserService currentUserService)
    {
        _documentService = documentService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DocumentDto>>> GetDocuments([FromQuery] DocumentFilterDto filter, CancellationToken cancellationToken)
    {
        filter.MaxAccessLevel ??= _currentUserService.MaxAllowedAccessLevel;
        var result = await _documentService.GetDocumentsAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _documentService.GetDocumentByIdAsync(id, _currentUserService.MaxAllowedAccessLevel, cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = "Документ не найден или недостаточно прав доступа" });
        }

        return Ok(result);
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<DocumentDto>> Upload([FromForm] IFormFile file, [FromForm] string? title, [FromForm] string? description, [FromForm] string? category, [FromForm] AccessLevel accessLevel = AccessLevel.PublicInternal, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Файл не прикреплен или пуст" });
        }

        var ownerId = _currentUserService.UserId ?? Guid.Parse("11111111-1111-1111-1111-111111111111");

        try
        {
            await using var stream = file.OpenReadStream();
            var request = new DocumentUploadRequestDto
            {
                Title = title ?? Path.GetFileNameWithoutExtension(file.FileName),
                Description = description ?? string.Empty,
                Category = category ?? "Общие",
                AccessLevel = accessLevel,
                OriginalFileName = file.FileName,
                ContentType = file.ContentType,
                FileStream = stream,
                FileLength = file.Length
            };

            var document = await _documentService.UploadDocumentAsync(request, ownerId, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = document.Id }, document);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? Guid.Parse("11111111-1111-1111-1111-111111111111");
        var userRole = _currentUserService.Role ?? UserRole.SystemAdmin;

        var success = await _documentService.DeleteDocumentAsync(id, userId, userRole, cancellationToken);
        if (!success)
        {
            return Forbid();
        }

        return NoContent();
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DocumentDto>> Update(Guid id, [FromBody] DocumentUpdateDto dto, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? Guid.Parse("11111111-1111-1111-1111-111111111111");
        var userRole = _currentUserService.Role ?? UserRole.SystemAdmin;

        var updated = await _documentService.UpdateDocumentMetadataAsync(id, dto, userId, userRole, cancellationToken);
        if (updated == null)
        {
            return NotFound(new { message = "Документ не найден или недостаточно прав для изменения" });
        }

        return Ok(updated);
    }


    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var result = await _documentService.DownloadDocumentAsync(id, _currentUserService.MaxAllowedAccessLevel, cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = "Файл не найден или доступ ограничен" });
        }

        return File(result.Value.FileStream, result.Value.ContentType, result.Value.FileName);
    }
}
