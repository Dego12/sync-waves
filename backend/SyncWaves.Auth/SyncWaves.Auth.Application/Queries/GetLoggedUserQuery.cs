using MediatR;
using SyncWaves.Auth.Application.DTOs;

namespace SyncWaves.Auth.Application.Queries;

public class GetLoggedUserQuery : IRequest<UserDTO>
{
    public Guid UserId { get; set; }
}
