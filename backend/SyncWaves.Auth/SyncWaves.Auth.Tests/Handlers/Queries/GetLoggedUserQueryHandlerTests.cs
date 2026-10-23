using Moq;
using SyncWaves.Auth.Application.Handlers.Queries;
using SyncWaves.Auth.Application.Queries;
using SyncWaves.Auth.Domain.Entities;
using SyncWaves.Auth.Domain.Exceptions;

namespace SyncWaves.Auth.UnitTests.Handlers;

public class GetLoggedUserQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserExists_ReturnsDto()
    {
        //Arrange
        var usersRepo = new Mock<IUsersRepository>(MockBehavior.Strict);

        var userId = Guid.NewGuid();
        var user = new UserEntity("andrei", "andrei@ye.com", "hash")
        {
            Id = userId,
            ProfilePictureUrl = "https://cdn/pic.png"
        };

        usersRepo
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new GetLoggedUserQueryHandler(usersRepo.Object);

        //Act
        var dto = await handler.Handle(new GetLoggedUserQuery { UserId = userId }, CancellationToken.None);

        //Assert
        Assert.Equal(userId, dto.Id);
        Assert.Equal(user.Email, dto.Email);
        Assert.Equal(user.ProfilePictureUrl, dto.ProfileImageUrl);

        usersRepo.Verify(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ThrowsUserNotFoundException()
    {
        //Arrange
        var usersRepo = new Mock<IUsersRepository>(MockBehavior.Strict);

        var userId = Guid.NewGuid();

        usersRepo
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEntity?)null);

        var handler = new GetLoggedUserQueryHandler(usersRepo.Object);

        //Act
        var ex = await Assert.ThrowsAsync<UserNotFoundException>(() =>
            handler.Handle(new GetLoggedUserQuery { UserId = userId }, CancellationToken.None));

        //Assert
        Assert.Equal("User_Does_Not_Exist", ex.Message);
    }
}
