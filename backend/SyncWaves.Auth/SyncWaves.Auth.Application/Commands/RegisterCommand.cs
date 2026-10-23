using MediatR;
using SyncWaves.Auth.Application.DTOs;

namespace SyncWaves.Auth.Application.Commands;

public class RegisterCommand : IRequest<UserDTO?>
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
