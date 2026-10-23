using MediatR;
using SyncWaves.Auth.Application.DTOs;

namespace SyncWaves.Auth.Application.Commands;

public class LoginWithMicrosoftCommand : IRequest<LoginDTO>
{
    public string IdToken { get; set; } = string.Empty;
}
