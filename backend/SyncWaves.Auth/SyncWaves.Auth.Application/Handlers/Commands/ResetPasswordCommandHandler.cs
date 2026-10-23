using MediatR;
using SyncWaves.Auth.Application.Abstractions;
using SyncWaves.Auth.Application.Commands;
using SyncWaves.Auth.Application.Resources;
using SyncWaves.Auth.Domain.Abstractions;
using SyncWaves.Auth.Domain.Exceptions;

namespace SyncWaves.Auth.Application.Handlers.Commands;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IUsersRepository _usersRepository;
    private readonly IPasswordService _passwordService;

    public ResetPasswordCommandHandler(IUsersRepository usersRepository,
        IPasswordService passwordService)
    {
        _usersRepository = usersRepository;
        _passwordService = passwordService;
    }

    public async Task Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await _usersRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user == null)
        {
            throw new UserNotFoundException("Invalid_Token");
        }

        if (user.PasswordResetToken != command.Token ||
            user.PasswordResetTokenExpiresAt is null ||
            user.PasswordResetTokenExpiresAt < DateTime.UtcNow)
        {
            throw new TokenInvalidException("Invalid_Expired_Token");
        }

        if (_passwordService.EnsureValid(command.Password))
        {
            var hash = _passwordService.Hash(command.Password);
            user.SetPasswordHash(hash);
            await _usersRepository.UpdateAsync(user, cancellationToken);
            await _usersRepository.SaveChangesAsync(cancellationToken);
        }
    }
}
