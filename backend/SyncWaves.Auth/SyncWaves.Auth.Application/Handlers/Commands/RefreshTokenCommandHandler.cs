using MediatR;
using SyncWaves.Auth.Application.Abstractions;
using SyncWaves.Auth.Application.Commands;
using SyncWaves.Auth.Application.DTOs;
using SyncWaves.Auth.Domain.Entities;
using SyncWaves.Auth.Domain.Exceptions;
using System.Security.Cryptography;
using System.Text;

namespace SyncWaves.Auth.Application.Handlers.Commands;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, LoginDTO>
{
    private readonly IRefreshTokensRepository _refreshTokensRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IUsersRepository _usersRepository;

    public RefreshTokenCommandHandler(IRefreshTokensRepository refreshTokensRepository, IJwtTokenGenerator jwtTokenGenerator, IUsersRepository usersRepository)
    {
        _refreshTokensRepository = refreshTokensRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _usersRepository = usersRepository;
    }

    public async Task<LoginDTO> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var existing = await _refreshTokensRepository.GetByTokenHashAsync(command.RawRefreshToken, cancellationToken)
                       ?? throw new TokenInvalidException("Invalid_Token");

        if (!existing.IsActive)
            throw new TokenInvalidException("Invalid_Token");

        var user = await _usersRepository.GetByIdAsync(existing.UserId, cancellationToken)
                   ?? throw new UserNotFoundException("User_Does_Not_Exist");

        var newJwt = _jwtTokenGenerator.GenerateToken(user, out var expiresAt);

        var newRaw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var newEntity = new RefreshTokenEntity
        {
            UserId = user.Id,
            TokenHash = _refreshTokensRepository.HashToken(newRaw),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        existing.RevokedAt = DateTime.UtcNow;
        existing.ReplacedByTokenHash = newEntity.TokenHash;

        await _refreshTokensRepository.AddAsync(newEntity, cancellationToken);
        await _refreshTokensRepository.SaveChangesAsync(cancellationToken);

        return new LoginDTO
        {
            AccessToken = newJwt,
            RefreshToken = newEntity.TokenHash,
            UserId = user.Id,
            Email = user.Email,
            AccessTokenExpiresAt = expiresAt
        };
    }
}
