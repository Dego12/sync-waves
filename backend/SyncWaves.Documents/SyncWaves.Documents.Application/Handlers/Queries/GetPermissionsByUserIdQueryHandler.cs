using MediatR;
using SyncWaves.Documents.Application.DTOs;
using SyncWaves.Documents.Application.Queries;
using SyncWaves.Documents.Application.Resources;
using SyncWaves.Documents.Domain.Abstractions;

namespace SyncWaves.Documents.Application.Handlers.Queries;

public class GetPermissionsByUserIdQueryHandler : IRequestHandler<GetPermissionsByUserIdQuery, DocumentPermissionsDTO>
{
    private readonly IDocumentPermissionsRepository _documentPermissionsRepository;

    public GetPermissionsByUserIdQueryHandler(IDocumentPermissionsRepository documentPermissionRepository)
    {
        _documentPermissionsRepository = documentPermissionRepository;
    }

    public async Task<DocumentPermissionsDTO> Handle(GetPermissionsByUserIdQuery query, CancellationToken cancellationToken)
    {
        var info = await _documentPermissionsRepository.GetPermissionsForUserAsync(query.DocumentId, query.UserId, cancellationToken);
        return new DocumentPermissionsDTO
        {
            IsOwner = info.IsOwner,
            CanEdit = info.CanEdit
        };
    }
}
