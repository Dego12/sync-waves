using Microsoft.EntityFrameworkCore;
using SyncWaves.Documents.Domain.Abstractions;
using SyncWaves.Documents.Domain.Entities;
using SyncWaves.Documents.Infrastructure.Data.Models;

namespace SyncWaves.Documents.Infrastructure.Data.Repositories;


public class DocumentPermissionsRepository : IDocumentPermissionsRepository
{
    private readonly DocumentsDbContext _dbContext;

    public DocumentPermissionsRepository(DocumentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DocumentCollaboratorEntity> GetPermissionsForUserAsync(Guid documentId, Guid userId, CancellationToken cancellationToken)
    {
        var doc = await _dbContext.Documents
            .AsNoTracking()
            .SingleOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        if (doc == null)
            throw new ArgumentException("Document_Not_Found");

        var isOwner = doc.UserId == userId;

        var collaborator = await _dbContext.Collaborators
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.DocumentId == documentId && c.UserId == userId);

        var canEdit = isOwner || (collaborator?.CanEdit ?? false);

        return new DocumentCollaboratorEntity
        {
            IsOwner = isOwner,
            CanEdit = canEdit,
            DocumentId = documentId,
            UserId = userId
        };
    }

    public async Task SetEditPermissionAsync(Guid documentId, Guid ownerId, Guid targetUserId, bool canEdit, CancellationToken cancellationToken)
    {
        var doc = await _dbContext.Documents
            .SingleOrDefaultAsync(d => d.Id == documentId);

        if (doc == null)
            throw new ArgumentException("Document_Not_Found");

        if (doc.UserId != ownerId)
            throw new UnauthorizedAccessException("Only_Owner_Changes");

        var collaborator = await _dbContext.Collaborators
            .SingleOrDefaultAsync(c => c.DocumentId == documentId && c.UserId == targetUserId);

        if (collaborator == null)
        {
            collaborator = new DocumentCollaboratorModel
            {
                DocumentId = documentId,
                UserId = targetUserId,
                CanEdit = canEdit
            };
            await _dbContext.Collaborators.AddAsync(collaborator, cancellationToken);
        }
        else
        {
            collaborator.CanEdit = canEdit;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
