using MediatR;
using SyncWaves.Auth.Application.DTOs;

namespace SyncWaves.Auth.Application.Commands;

public class LoginCommand : IRequest<LoginDTO?>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}
