using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SyncWaves.Auth.Application.Abstractions;
using SyncWaves.Auth.Application.Commands;
using SyncWaves.Auth.Application.DTOs;
using SyncWaves.Auth.Domain.Entities;
using SyncWaves.Auth.Domain.Exceptions;
using System.Security.Authentication;
using System.Security.Cryptography;

namespace SyncWaves.Auth.Application.Handlers.Commands;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginDTO?>
{
    private readonly IUsersRepository _usersRepository;
    private readonly IPasswordService _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokensRepository _refreshTokensRepository;

    public LoginCommandHandler(IUsersRepository usersRepository,
        IPasswordService passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokensRepository resfreshTokenRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _jwtTokenGenerator = jwtTokenGenerator;
        _usersRepository = usersRepository;
        _passwordHasher = passwordHasher;
        _refreshTokensRepository = resfreshTokenRepository;
    }

    public async Task<LoginDTO?> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var user = await _usersRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (user == null)
        {
            throw new UserNotFoundException("Login_Invalid_Credentials");
        }

        if (!_passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            throw new InvalidCredentialException("Login_Invalid_Credentials");
        }

        if (!user.EmailConfirmed)
        {
            throw new EmailNotConfirmedException("Login_Email_Not_Confirmed");
        }

        var jwt = _jwtTokenGenerator.GenerateToken(user, out var expiresAt);

        var refreshRaw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshEntity = new RefreshTokenEntity
        {
            UserId = user.Id,
            TokenHash = _refreshTokensRepository.HashToken(refreshRaw),
            ExpiresAt = command.RememberMe ? expiresAt.AddDays(7) : expiresAt
        };

        await _refreshTokensRepository.AddAsync(refreshEntity, cancellationToken);
        await _refreshTokensRepository.SaveChangesAsync(cancellationToken);

        return new LoginDTO { 
            AccessToken = jwt,
            RefreshToken = refreshEntity.TokenHash,
            UserId = user.Id,
            Email = user.Email,
            AccessTokenExpiresAt = expiresAt
        };
    }
}
