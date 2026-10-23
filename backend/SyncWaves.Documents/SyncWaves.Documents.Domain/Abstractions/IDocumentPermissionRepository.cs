using SyncWaves.Documents.Domain.Entities;

namespace SyncWaves.Documents.Domain.Abstractions;

public interface IDocumentPermissionsRepository
{
    Task<DocumentCollaboratorEntity> GetPermissionsForUserAsync(Guid documentId, Guid userId, CancellationToken cancellationToken = default);
    Task SetEditPermissionAsync(Guid documentId, Guid ownerId, Guid targetUserId, bool canEdit, CancellationToken cancellationToken = default);
}
