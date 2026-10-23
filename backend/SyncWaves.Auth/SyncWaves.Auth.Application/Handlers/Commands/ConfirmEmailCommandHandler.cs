using MediatR;
using SyncWaves.Auth.Application.Commands;
using SyncWaves.Auth.Domain.Exceptions;

namespace SyncWaves.Auth.Application.Handlers.Commands;

public class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand>
{
    private readonly IUsersRepository _userRepository;

    public ConfirmEmailCommandHandler(IUsersRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task Handle(ConfirmEmailCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user == null)
        {
            throw new UserNotFoundException("Invalid_Token");
        }

        if (user.EmailConfirmed)
        {
            throw new EmailAlreadyConfirmedException("Confirm_Email_Already_Confirmed");
        }

        if (user.EmailConfirmationToken != command.Token ||
            user.EmailConfirmationTokenExpiresAt is null ||
            user.EmailConfirmationTokenExpiresAt < DateTime.UtcNow)
        {
            throw new TokenInvalidException("Invalid_Expired_Token");
        }

        user.ConfirmEmail();
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);
    }
}
