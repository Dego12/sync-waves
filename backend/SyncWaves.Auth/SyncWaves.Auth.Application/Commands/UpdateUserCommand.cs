using MediatR;
using Microsoft.AspNetCore.Http;
using SyncWaves.Auth.Application.DTOs;

namespace SyncWaves.Auth.Application.Commands;

public class UpdateUserCommand : IRequest<UserDTO>
{
    public IFormFile FormFile { get; set; } = default!;
    public Guid UserId { get; set; }
}
