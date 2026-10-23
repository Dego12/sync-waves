using MediatR;
using SyncWaves.Auth.Application.DTOs;
using SyncWaves.Auth.Application.Queries;
using SyncWaves.Auth.Application.Resources;
using SyncWaves.Auth.Domain.Exceptions;

namespace SyncWaves.Auth.Application.Handlers.Queries;

public class GetLoggedUserQueryHandler : IRequestHandler<GetLoggedUserQuery, UserDTO>
{
    private readonly IUsersRepository _usersRepository;

    public GetLoggedUserQueryHandler(IUsersRepository usersRepository)
    {
        _usersRepository = usersRepository;
    }

    public async Task<UserDTO> Handle(GetLoggedUserQuery query, CancellationToken cancellationToken)
    {
        var user = await _usersRepository.GetByIdAsync(query.UserId, cancellationToken);

        if (user == null)
        {
            throw new UserNotFoundException("User_Does_Not_Exist");
        }
        return new UserDTO
        {
            Id = user.Id,
            Email = user.Email,
            ProfileImageUrl = user.ProfilePictureUrl
        };
    }
}
