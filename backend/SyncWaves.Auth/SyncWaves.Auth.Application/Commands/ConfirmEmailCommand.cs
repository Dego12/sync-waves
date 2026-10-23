using MediatR;

namespace SyncWaves.Auth.Application.Commands;

public class ConfirmEmailCommand : IRequest
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
}
