using MediatR;

namespace SyncWaves.Auth.Application.Commands;

public class ResetPasswordCommand : IRequest
{
    public Guid UserId { get; set; }
    public string Password { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}
