using MediatR;
using SyncWaves.Documents.Application.DTOs;

namespace SyncWaves.Documents.Application.Queries;

public class GetPermissionsByUserIdQuery : IRequest<DocumentPermissionsDTO>
{
    public Guid DocumentId { get; set; }
    public Guid UserId { get; set; }
}
