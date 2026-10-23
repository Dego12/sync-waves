using MediatR;
using SyncWaves.Auth.Application.Commands;
using SyncWaves.Auth.Application.DTOs;
using SyncWaves.Auth.Application.Resources;
using SyncWaves.Auth.Domain.Abstractions;
using SyncWaves.Auth.Domain.Exceptions;

namespace SyncWaves.Auth.Application.Handlers.Commands;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserDTO>
{
    private readonly IUsersRepository _usersRepository;
    private readonly IProfileImageStorage _profileImageStorage;
    private string[] AllowedTypes = { "image/png", "image/jpeg", "image/jpg", "image/webp" };

    public UpdateUserCommandHandler(IUsersRepository usersRepository, IProfileImageStorage profileImageStorage)
    {
        _profileImageStorage = profileImageStorage;
        _usersRepository = usersRepository;
    }

    public async Task<UserDTO> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        if (command.FormFile == null || command.FormFile.Length == 0)
        {
            throw new ArgumentException("No_File");
        }

        if (!AllowedTypes.Contains(command.FormFile.ContentType))
        {
            throw new ArgumentException("Invalid_File_Type");
        }

        if (command.FormFile.Length > 5 * 1024 * 1024)
        {
            throw new ArgumentException("File_Too_Large");
        }

        await using var stream = command.FormFile.OpenReadStream();

        var url = await _profileImageStorage.SaveProfileImageAsync(command.UserId, stream, command.FormFile.FileName, command.FormFile.ContentType, cancellationToken);
        var user = await _usersRepository.GetByIdAsync(command.UserId);

        if (user == null)
        {
            throw new UserNotFoundException("User_Not_Found");
        }

        user.SetProfilePicture(url);
        await _usersRepository.UpdateAsync(user);
        await _usersRepository.SaveChangesAsync(cancellationToken);

        return new UserDTO()
        {
            Id = user.Id,
            Email = user.Email,
            ProfileImageUrl = user.ProfilePictureUrl
        };
    }
}
