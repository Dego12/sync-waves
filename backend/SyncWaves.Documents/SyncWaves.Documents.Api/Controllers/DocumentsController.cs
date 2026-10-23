using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncWaves.Documents.Api.Responses;
using SyncWaves.Documents.Application.Commands;
using SyncWaves.Documents.Application.DTOs;
using SyncWaves.Documents.Application.Queries;
using SyncWaves.Documents.Domain.Exceptions;
using System.Security.Claims;

namespace SyncWaves.Documents.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DocumentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets a document based on its Id
    /// </summary>
    /// <param name="documentId"></param>
    /// <returns>Return the document if found.</returns>
    [HttpGet("{documentId:guid}")]
    [Authorize]
    public async Task<ActionResult<DocumentDTO?>> GetById(Guid documentId)
    {
        var query = new GetDocumentQuery() { Id = documentId };
        var document = await _mediator.Send(query);

        return Ok(document);
    }

    /// <summary>
    /// Gets documents based on user.
    /// </summary>
    /// <returns>Return the documents if found.</returns>
    [HttpGet("mine")]
    [Authorize]
    public async Task<ActionResult<List<DocumentDTO>>> GetMine(CancellationToken ct)
    {

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var ownerId = Guid.Parse(userIdStr!);

        var docs = await _mediator.Send(new GetDocumentsByUserIdQuery { UserId = ownerId });

        return Ok(docs);
    }

    /// <summary>
    /// Creates a document
    /// </summary>
    /// <param name="command">Document attributes for the creation operation.</param>
    /// <returns>Return the document if created.</returns>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<DocumentDTO>> Create([FromBody] CreateDocumentCommand command)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var ownerId = Guid.Parse(userIdStr!);
        command.UserId = ownerId;

        var dto = await _mediator.Send(command);
        await _mediator.Send(new UpdatePermissionCommand
        {
            DocumentId = dto.Id,
            OwnerId = ownerId,
            CanEdit = true,
            UserId = ownerId
        });

        return Ok(dto);
    }

    /// <summary>
    /// Updates a document
    /// </summary>
    /// <param name="command">Document attributes for the update operation.</param>
    /// <returns>None</returns>
    [HttpPut]
    [Authorize]
    public async Task<IActionResult> Update([FromBody] UpdateDocumentCommand command)
    {
        try
        {
            await _mediator.Send(command);

            return Ok();
        }
        catch (DocumentConflictException ex)
        {
            return Conflict(new DocumentConflictResponse
            {
                Current = new DocumentDTO
                {
                    Id = ex.CurrentDocument.Id,
                    Title = ex.CurrentDocument.Title,
                    Content = ex.CurrentDocument.Content,
                    Version = ex.CurrentDocument.Version,
                    CreatedAt = ex.CurrentDocument.CreatedAt,
                    UpdatedAt = ex.CurrentDocument.UpdatedAt,
                    UserId = ex.CurrentDocument.UserId
                }
            });
        }
    }

    [HttpPost("export/pdf")]
    [Authorize]
    public async Task<IActionResult> ExportPdf(ExportPdfCommand command)
    {
        var result = await _mediator.Send(command);

        if (result.PdfBytes != null)
        {
            return File(result.PdfBytes, "application/pdf", result.FileName);
        }
        return NotFound(result.FileName);
    }

    [HttpGet("{documentId:guid}/permissions/me")]
    public async Task<IActionResult> GetMyPermissions(Guid documentId)
    {

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var info = await _mediator.Send(new GetPermissionsByUserIdQuery { DocumentId = documentId, UserId = Guid.Parse(userId) });

        return Ok(info);
    }


    [HttpPost("{documentId:guid}/permissions")]
    public async Task<IActionResult> SetPermission(Guid documentId, [FromBody] UpdatePermissionDTO updatePermission)
    {
        var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _mediator.Send(new UpdatePermissionCommand
        {
            OwnerId = Guid.Parse(ownerId),
            CanEdit = updatePermission.CanEdit,
            UserId = updatePermission.UserId,
            DocumentId = documentId
        });
        return NoContent();
    }
}
