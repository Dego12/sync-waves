using MediatR;
using SyncWaves.Auth.Application.Abstractions;
using SyncWaves.Auth.Application.Commands;
using SyncWaves.Auth.Application.DTOs;
using SyncWaves.Auth.Domain.Abstractions;
using SyncWaves.Auth.Domain.Entities;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Cryptography;

namespace SyncWaves.Auth.Application.Handlers.Commands;

public class LoginWithMicrosoftCommandHandler : IRequestHandler<LoginWithMicrosoftCommand, LoginDTO>
{
    private readonly IMicrosoftTokenValidator _tokenValidator;
    private readonly IUsersRepository _usersRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokensRepository _refreshTokensRepository;

    public LoginWithMicrosoftCommandHandler(
        IMicrosoftTokenValidator tokenValidator,
        IUsersRepository usersRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokensRepository refreshTokensRepository)
    {
        _tokenValidator = tokenValidator;
        _usersRepository = usersRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokensRepository = refreshTokensRepository;
    }

    public async Task<LoginDTO> Handle(LoginWithMicrosoftCommand request, CancellationToken cancellationToken)
    {
        var principal = await _tokenValidator.ValidateIdTokenAsync(request.IdToken, cancellationToken);

        var externalId = principal.FindFirst("oid") ?? principal.FindFirst("sub") 
            ?? throw new InvalidCredentialException("Invalid Microsoft token."); 
        
        var email = principal.FindFirst(ClaimTypes.Email) 
            ?? principal.FindFirst("preferred_username"); 
        
        var name = principal.FindFirst("name") ?? email;

        var user = await _usersRepository.GetByExternalIdAsync("Microsoft", externalId.Value, cancellationToken);
        if (user == null)
        {
            var newUser = new UserEntity()
            {
                Id = Guid.NewGuid(),
                Email = email?.Value ?? $"{externalId}@unknown.microsoft",
                PasswordHash = string.Empty,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                ExternalProvider = "Microsoft",
                ExternalId = externalId.Value
            };

            user = newUser;

            await _usersRepository.AddAsync(newUser, cancellationToken);
            await _usersRepository.SaveChangesAsync(cancellationToken);
        }

        var accessToken = _jwtTokenGenerator.GenerateToken(user, out var expiresAt);

        var refreshRaw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshEntity = new RefreshTokenEntity
        {
            UserId = user.Id,
            TokenHash = _refreshTokensRepository.HashToken(refreshRaw),
            ExpiresAt = expiresAt
        };

        await _refreshTokensRepository.AddAsync(refreshEntity, cancellationToken);
        await _refreshTokensRepository.SaveChangesAsync(cancellationToken);

        return new LoginDTO
        {
            AccessToken = accessToken,
            RefreshToken = refreshEntity.TokenHash,
            Email = user.Email,
            UserId = user.Id,
            AccessTokenExpiresAt = expiresAt
        };
    }
}

