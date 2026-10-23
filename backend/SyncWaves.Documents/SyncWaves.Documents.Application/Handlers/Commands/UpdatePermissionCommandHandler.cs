using MediatR;
using SyncWaves.Documents.Application.Commands;
using SyncWaves.Documents.Application.Resources;
using SyncWaves.Documents.Domain.Abstractions;

namespace SyncWaves.Documents.Application.Handlers.Commands;

public class UpdatePermissionCommandHandler : IRequestHandler<UpdatePermissionCommand>
{
    private readonly IDocumentPermissionsRepository _documentPermissionRepository;

    public UpdatePermissionCommandHandler(IDocumentPermissionsRepository documentPermissionsRepository)
    {
        _documentPermissionRepository = documentPermissionsRepository;
    }

    public async Task Handle(UpdatePermissionCommand command, CancellationToken cancellationToken)
    {
        await _documentPermissionRepository.SetEditPermissionAsync(command.DocumentId, command.OwnerId, command.UserId, command.CanEdit, cancellationToken);
    }
}
