using Moq;
using SyncWaves.Auth.Application.Commands;
using SyncWaves.Auth.Application.Handlers.Commands;
using SyncWaves.Auth.Domain.Entities;
using SyncWaves.Auth.Domain.Exceptions;

namespace SyncWaves.Auth.UnitTests.Handlers.Commands;

public class ConfirmEmailCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsUserNotFoundException()
    {
        //Arrange
        var repo = new Mock<IUsersRepository>(MockBehavior.Strict);

        repo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEntity?)null);

        var handler = new ConfirmEmailCommandHandler(repo.Object);

        //Act
        var ex = await Assert.ThrowsAsync<UserNotFoundException>(() =>
            handler.Handle(new ConfirmEmailCommand { UserId = Guid.NewGuid(), Token = "t" }, CancellationToken.None));

        //Assert
        Assert.Equal("Invalid_Token", ex.Message);

        repo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.UpdateAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAlreadyConfirmed_ThrowsEmailAlreadyConfirmedException()
    {
        //Arrange
        var repo = new Mock<IUsersRepository>(MockBehavior.Strict);

        var user = new UserEntity("andrei", "andrei@ye.com", "hash")
        {
            EmailConfirmed = true
        };

        repo
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new ConfirmEmailCommandHandler(repo.Object);

        //Act
        var ex = await Assert.ThrowsAsync<EmailAlreadyConfirmedException>(() =>
            handler.Handle(new ConfirmEmailCommand { UserId = user.Id, Token = "t" }, CancellationToken.None));

        //Assert
        Assert.Equal("Confirm_Email_Already_Confirmed", ex.Message);

        repo.Verify(r => r.UpdateAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTokenDoesNotMatch_ThrowsTokenInvalidException()
    {
        //Arrange
        var repo = new Mock<IUsersRepository>(MockBehavior.Strict);

        var user = new UserEntity("andrei", "andrei@ye.com", "hash");
        user.SetEmailConfirmation("correct", DateTime.UtcNow.AddHours(1));

        repo
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new ConfirmEmailCommandHandler(repo.Object);

        //Act
        var ex = await Assert.ThrowsAsync<TokenInvalidException>(() =>
            handler.Handle(new ConfirmEmailCommand { UserId = user.Id, Token = "wrong" }, CancellationToken.None));

        //Assert
        Assert.Equal("Invalid_Expired_Token", ex.Message);
    }

    [Fact]
    public async Task Handle_WhenTokenExpired_ThrowsTokenInvalidException()
    {
        //Arrange
        var repo = new Mock<IUsersRepository>(MockBehavior.Strict);

        var user = new UserEntity("andrei", "andrei@ye.com", "hash");
        user.SetEmailConfirmation("token", DateTime.UtcNow.AddMinutes(-1));

        repo
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new ConfirmEmailCommandHandler(repo.Object);

        //Act
        var ex = await Assert.ThrowsAsync<TokenInvalidException>(() =>
            handler.Handle(new ConfirmEmailCommand { UserId = user.Id, Token = "token" }, CancellationToken.None));

        //Assert
        Assert.Equal("Invalid_Expired_Token", ex.Message);
    }

    [Fact]
    public async Task Handle_WhenValid_ConfirmsEmail_UpdatesAndSaves()
    {
        //Arrange
        var repo = new Mock<IUsersRepository>(MockBehavior.Strict);

        var user = new UserEntity("andrei", "andrei@ye.com", "hash");
        user.SetEmailConfirmation("token", DateTime.UtcNow.AddHours(1));

        repo
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        repo
            .Setup(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        repo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new ConfirmEmailCommandHandler(repo.Object);

        //Act
        await handler.Handle(new ConfirmEmailCommand { UserId = user.Id, Token = "token" }, CancellationToken.None);

        //Assert
        Assert.True(user.EmailConfirmed);
        Assert.Null(user.EmailConfirmationToken);
        Assert.Null(user.EmailConfirmationTokenExpiresAt);

        repo.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
