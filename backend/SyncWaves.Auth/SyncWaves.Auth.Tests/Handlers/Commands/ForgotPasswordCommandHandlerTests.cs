using Moq;
using SyncWaves.Auth.Application.Commands;
using SyncWaves.Auth.Application.Handlers.Commands;
using SyncWaves.Auth.Application.Resources;
using SyncWaves.Auth.Domain.Abstractions;
using SyncWaves.Auth.Domain.Entities;
using SyncWaves.Auth.Domain.Exceptions;

namespace SyncWaves.Auth.UnitTests.Handlers.Commands;

public class ForgotPasswordCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ThrowsUserNotFoundException()
    {
        //Arrange
        var repo = new Mock<IUsersRepository>(MockBehavior.Strict);
        var email = new Mock<IEmailService>(MockBehavior.Strict);
        var localizer = new Mock<IResourceLocalizer>(MockBehavior.Strict);

        repo
            .Setup(r => r.GetByEmailAsync("missing@ye.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEntity?)null);

        var handler = new ForgotPasswordCommandHandler(
            repo.Object,
            email.Object,
            localizer.Object,
            TestHelpers.BuildConfiguration("https://frontend.example"));

        //Act
        var ex = await Assert.ThrowsAsync<UserNotFoundException>(() =>
            handler.Handle(new ForgotPasswordCommand { Email = "missing@ye.com" }, CancellationToken.None));

        //Assert
        Assert.Equal("User_Does_Not_Exist", ex.Message);

        repo.Verify(r => r.UpdateAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        email.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenValid_SetsResetToken_Persists_AndSendsEmail()
    {
        //Arrange
        var repo = new Mock<IUsersRepository>(MockBehavior.Strict);
        var email = new Mock<IEmailService>(MockBehavior.Strict);
        var localizer = new Mock<IResourceLocalizer>(MockBehavior.Strict);

        localizer
            .Setup(l => l.Localize("Reset_Password_Subject"))
            .Returns(TestHelpers.ResxHelper["Reset_Password_Subject"]);

        localizer
            .Setup(l => l.Localize("Reset_Password_Html"))
            .Returns(TestHelpers.ResxHelper["Reset_Password_Html"]);

        var user = new UserEntity("andrei", "andrei@ye.com", "hash") { EmailConfirmed = true };

        repo
            .Setup(r => r.GetByEmailAsync("andrei@ye.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        repo
            .Setup(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        repo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        string? capturedBody = null;
        email
            .Setup(s => s.SendAsync("andrei@ye.com", TestHelpers.ResxHelper["Reset_Password_Subject"], It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, __, body, ___) => capturedBody = body)
            .Returns(Task.CompletedTask);

        var handler = new ForgotPasswordCommandHandler(
            repo.Object,
            email.Object,
            localizer.Object,
            TestHelpers.BuildConfiguration("https://frontend.example"));

        //Act
        await handler.Handle(new ForgotPasswordCommand { Email = "andrei@ye.com" }, CancellationToken.None);

        //Assert
        Assert.False(string.IsNullOrWhiteSpace(user.PasswordResetToken));
        Assert.NotNull(user.PasswordResetTokenExpiresAt);

        repo.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        email.Verify(s => s.SendAsync(
            "andrei@ye.com",
            TestHelpers.ResxHelper["Reset_Password_Subject"],
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);

        Assert.NotNull(capturedBody);
        Assert.Contains("https://frontend.example/reset-password?userId=", capturedBody);
        Assert.Contains("&token=", capturedBody);
    }
}
