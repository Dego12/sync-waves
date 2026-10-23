using MediatR;
using Microsoft.Extensions.Configuration;
using SyncWaves.Auth.Application.Abstractions;
using SyncWaves.Auth.Application.Commands;
using SyncWaves.Auth.Application.DTOs;
using SyncWaves.Auth.Application.Resources;
using SyncWaves.Auth.Domain.Abstractions;
using SyncWaves.Auth.Domain.Entities;
using System.Security.Cryptography;
using System.Web;

namespace SyncWaves.Auth.Application.Handlers.Commands;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, UserDTO?>
{
    private readonly IUsersRepository _usersRepository;
    private readonly IPasswordService _passwordService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly IResourceLocalizer _localizer;

    public RegisterCommandHandler(IUsersRepository userRepository,
        IPasswordService passwordService,
        IEmailService emailService,
        IConfiguration configuration,
        IResourceLocalizer localizer)
    {
        _usersRepository = userRepository;
        _passwordService = passwordService;
        _emailService = emailService;
        _configuration = configuration;
        _localizer = localizer;
    }

    public async Task<UserDTO?> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        if (await _usersRepository.GetByEmailAsync(command.Email, cancellationToken) != null)
        {
            throw new ArgumentException("Register_Email_Already_Registered");
        }

        if (_passwordService.EnsureValid(command.Password))
        {
            var hash = _passwordService.Hash(command.Password);
            var user = new UserEntity(command.Name, command.Email, hash);

            var bytes = RandomNumberGenerator.GetBytes(32);
            var token = Convert.ToBase64String(bytes);
            user.SetEmailConfirmation(token, DateTime.UtcNow.AddHours(24));

            await _usersRepository.AddAsync(user, cancellationToken);
            await _usersRepository.SaveChangesAsync(cancellationToken);

            var urlToken = HttpUtility.UrlEncode(token);
            var frontendBaseUrl = _configuration["App:FrontendBaseUrl"];
            var verifyUrl = $"{frontendBaseUrl}/verify-email?userId={user.Id}&token={urlToken}";

            var emailBody = string.Format(_localizer.Localize("Register_Email_Confirmation_Html"), verifyUrl);

            await _emailService.SendAsync(user.Email, _localizer.Localize("Register_Email_Confirmation_Subject"), emailBody, cancellationToken);

            return new UserDTO
            {
                Id = user.Id,
                Email = user.Email
            };
        }
        return null;
    }
}
