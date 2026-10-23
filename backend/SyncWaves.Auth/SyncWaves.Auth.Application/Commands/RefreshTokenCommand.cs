using MediatR;
using SyncWaves.Auth.Application.DTOs;

namespace SyncWaves.Auth.Application.Commands;

public class RefreshTokenCommand : IRequest<LoginDTO>
{
    public string RawRefreshToken { get; set; } = string.Empty;
}
