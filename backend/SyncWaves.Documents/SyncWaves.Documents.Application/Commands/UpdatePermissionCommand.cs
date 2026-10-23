using MediatR;

namespace SyncWaves.Documents.Application.Commands;

public class UpdatePermissionDTO
{
    public Guid UserId { get; set; }
    public bool CanEdit { get; set; }
}

public class UpdatePermissionCommand : IRequest
{
    public Guid UserId { get; set; }
    public Guid OwnerId { get; set; }
    public Guid DocumentId { get; set; }
    public bool CanEdit { get; set; }
}
