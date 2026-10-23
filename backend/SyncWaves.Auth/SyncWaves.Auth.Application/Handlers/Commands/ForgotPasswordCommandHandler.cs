using MediatR;
using Microsoft.Extensions.Configuration;
using SyncWaves.Auth.Application.Commands;
using SyncWaves.Auth.Application.Resources;
using SyncWaves.Auth.Domain.Abstractions;
using SyncWaves.Auth.Domain.Exceptions;
using System.Security.Cryptography;
using System.Web;

namespace SyncWaves.Auth.Application.Handlers.Commands;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly IUsersRepository _usersRepository;
    private readonly IEmailService _emailService;
    private readonly IResourceLocalizer _localizer;
    private readonly IConfiguration _configuration;

    public ForgotPasswordCommandHandler(IUsersRepository usersRepository,
        IEmailService emailService,
        IResourceLocalizer localizer,
        IConfiguration configuration)
    {
        _usersRepository = usersRepository;
        _emailService = emailService;
        _localizer = localizer;
        _configuration = configuration;
    }

    public async Task Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await _usersRepository.GetByEmailAsync(command.Email, cancellationToken);

        if (user == null)
        {
            throw new UserNotFoundException("User_Does_Not_Exist");
        }

        var bytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(bytes);
        user.SetPasswordResetToken(token, DateTime.UtcNow.AddHours(24));

        await _usersRepository.UpdateAsync(user, cancellationToken);
        await _usersRepository.SaveChangesAsync(cancellationToken);

        var urlToken = HttpUtility.UrlEncode(token);
        var frontendBaseUrl = _configuration["App:FrontendBaseUrl"];
        var resetUrl = $"{frontendBaseUrl}/reset-password?userId={user.Id}&token={urlToken}";

        var emailBody = string.Format(_localizer.Localize("Reset_Password_Html"), resetUrl);

        await _emailService.SendAsync(user.Email, _localizer.Localize("Reset_Password_Subject"), emailBody, cancellationToken);
    }
}
