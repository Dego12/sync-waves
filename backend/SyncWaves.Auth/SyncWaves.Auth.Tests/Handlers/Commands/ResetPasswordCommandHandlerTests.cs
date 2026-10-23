using Moq;
using SyncWaves.Auth.Application.Abstractions;
using SyncWaves.Auth.Application.Commands;
using SyncWaves.Auth.Application.Handlers.Commands;
using SyncWaves.Auth.Domain.Entities;
using SyncWaves.Auth.Domain.Exceptions;

namespace SyncWaves.Auth.UnitTests.Handlers.Commands;

public class ResetPasswordCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsUserNotFoundException()
    {
        //Arrange
        var repo = new Mock<IUsersRepository>(MockBehavior.Strict);
        var password = new Mock<IPasswordService>(MockBehavior.Strict);

        repo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEntity?)null);

        var handler = new ResetPasswordCommandHandler(repo.Object, password.Object);

        //Act
        var ex = await Assert.ThrowsAsync<UserNotFoundException>(() =>
            handler.Handle(new ResetPasswordCommand { UserId = Guid.NewGuid(), Token = "t", Password = "pw" }, CancellationToken.None));

        //Assert
        Assert.Equal("Invalid_Token", ex.Message);

        repo.Verify(r => r.UpdateAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTokenInvalid_ThrowsTokenInvalidException()
    {
        //Arrange
        var repo = new Mock<IUsersRepository>(MockBehavior.Strict);
        var password = new Mock<IPasswordService>(MockBehavior.Strict);

        var user = new UserEntity("Andrei", "andrei@ye.com", "hash");
        user.SetPasswordResetToken("correct", DateTime.UtcNow.AddHours(1));

        repo
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new ResetPasswordCommandHandler(repo.Object, password.Object);

        //Act
        var ex = await Assert.ThrowsAsync<TokenInvalidException>(() =>
            handler.Handle(new ResetPasswordCommand { UserId = user.Id, Token = "wrong", Password = "new" }, CancellationToken.None));

        //Assert
        Assert.Equal("Invalid_Expired_Token", ex.Message);
    }

    [Fact]
    public async Task Handle_WhenTokenExpired_ThrowsTokenInvalidException()
    {
        //Arrange
        var repo = new Mock<IUsersRepository>(MockBehavior.Strict);
        var password = new Mock<IPasswordService>(MockBehavior.Strict);

        var user = new UserEntity("andrei", "andrei@ye.com", "hash");
        user.SetPasswordResetToken("token", DateTime.UtcNow.AddMinutes(-1));

        repo
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new ResetPasswordCommandHandler(repo.Object, password.Object);

        //Act
        var ex = await Assert.ThrowsAsync<TokenInvalidException>(() =>
            handler.Handle(new ResetPasswordCommand { UserId = user.Id, Token = "token", Password = "new" }, CancellationToken.None));

        //Assert
        Assert.Equal("Invalid_Expired_Token", ex.Message);
    }

    [Fact]
    public async Task Handle_WhenPasswordValidationThrows_PropagatesException_AndDoesNotPersist()
    {
        //Arrange
        var repo = new Mock<IUsersRepository>(MockBehavior.Strict);
        var password = new Mock<IPasswordService>(MockBehavior.Strict);

        var user = new UserEntity("andrei", "andrei@ye.com", "oldhash");
        user.SetPasswordResetToken("token", DateTime.UtcNow.AddHours(1));

        repo
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        password
            .Setup(p => p.EnsureValid("bad"))
            .Throws(new ArgumentException("Password_Min_Length"));

        var handler = new ResetPasswordCommandHandler(repo.Object, password.Object);

        //Act
        var act = () => handler.Handle(new ResetPasswordCommand { UserId = user.Id, Token = "token", Password = "bad" }, CancellationToken.None);

        //Assert
        await Assert.ThrowsAsync<ArgumentException>(act);

        repo.Verify(r => r.UpdateAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenValid_UpdatesPasswordAndSaves()
    {
        //Arrange
        var repo = new Mock<IUsersRepository>(MockBehavior.Strict);
        var password = new Mock<IPasswordService>(MockBehavior.Strict);

        var user = new UserEntity("andrei", "andrei@ye.com", "oldhash");
        user.SetPasswordResetToken("token", DateTime.UtcNow.AddHours(1));

        repo
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        repo
            .Setup(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        repo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        password
            .Setup(p => p.EnsureValid("NewParola123!"))
            .Returns(true);

        password
            .Setup(p => p.Hash("NewParola123!"))
            .Returns("newhash");

        var handler = new ResetPasswordCommandHandler(repo.Object, password.Object);

        //Act
        await handler.Handle(new ResetPasswordCommand { UserId = user.Id, Token = "token", Password = "NewParola123!" }, CancellationToken.None);

        //Assert
        Assert.Equal("newhash", user.PasswordHash);

        repo.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
