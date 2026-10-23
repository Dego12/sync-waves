using Moq;
using SyncWaves.Auth.Application.Abstractions;
using SyncWaves.Auth.Application.Commands;
using SyncWaves.Auth.Application.Handlers.Commands;
using SyncWaves.Auth.Application.Resources;
using SyncWaves.Auth.Domain.Abstractions;
using SyncWaves.Auth.Domain.Entities;

namespace SyncWaves.Auth.UnitTests.Handlers.Commands;

public class RegisterCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenEmailAlreadyRegistered_ThrowsArgumentException()
    {
        //Arrange
        var usersRepo = new Mock<IUsersRepository>(MockBehavior.Strict);
        var passwordService = new Mock<IPasswordService>(MockBehavior.Strict);
        var emailService = new Mock<IEmailService>(MockBehavior.Strict);
        var localizer = new Mock<IResourceLocalizer>(MockBehavior.Strict);

        usersRepo
            .Setup(r => r.GetByEmailAsync("andrei@ye.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserEntity("Andrei", "andrei@ye.com", "hash"));

        var handler = new RegisterCommandHandler(
            usersRepo.Object,
            passwordService.Object,
            emailService.Object,
            TestHelpers.BuildConfiguration(),
            localizer.Object);

        //Act
        var cmd = new RegisterCommand { Name = "Andrei", Email = "andrei@ye.com", Password = "Password123!" };
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(cmd, CancellationToken.None));

        //Assert
        Assert.Equal("Register_Email_Already_Registered", ex.Message);

        usersRepo.Verify(r => r.AddAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        usersRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        emailService.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPasswordValidationThrows_PropagatesException_AndDoesNotPersist()
    {
        //Arrange
        var usersRepo = new Mock<IUsersRepository>(MockBehavior.Strict);
        var passwordService = new Mock<IPasswordService>(MockBehavior.Strict);
        var emailService = new Mock<IEmailService>(MockBehavior.Strict);
        var localizer = new Mock<IResourceLocalizer>(MockBehavior.Strict);

        usersRepo
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEntity?)null);

        passwordService
            .Setup(p => p.EnsureValid("bad"))
            .Throws(new ArgumentException("Password_Min_Length"));

        var handler = new RegisterCommandHandler(
            usersRepo.Object,
            passwordService.Object,
            emailService.Object,
            TestHelpers.BuildConfiguration(),
            localizer.Object);

        //Act
        var act = () => handler.Handle(new RegisterCommand { Name = "Andrei", Email = "andrei@ye.com", Password = "bad" }, CancellationToken.None);

        //Assert
        await Assert.ThrowsAsync<ArgumentException>(act);

        usersRepo.Verify(r => r.AddAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        usersRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        emailService.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenValidRequest_PersistsUser_SendsEmail_AndReturnsUserDto()
    {
        //Arrange
        var usersRepo = new Mock<IUsersRepository>(MockBehavior.Strict);
        var passwordService = new Mock<IPasswordService>(MockBehavior.Strict);
        var emailService = new Mock<IEmailService>(MockBehavior.Strict);
        var localizer = new Mock<IResourceLocalizer>(MockBehavior.Strict);

        usersRepo
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEntity?)null);

        passwordService
            .Setup(p => p.EnsureValid(It.IsAny<string>()))
            .Returns(true);

        passwordService
            .Setup(p => p.Hash("Parola123!"))
            .Returns("hashed");

        localizer
            .Setup(l => l.Localize("Register_Email_Confirmation_Subject"))
            .Returns(TestHelpers.ResxHelper["Register_Email_Confirmation_Subject"]);

        localizer
            .Setup(l => l.Localize("Register_Email_Confirmation_Html"))
            .Returns(TestHelpers.ResxHelper["Register_Email_Confirmation_Html"]);

        UserEntity? capturedUser = null;
        usersRepo
            .Setup(r => r.AddAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()))
            .Callback<UserEntity, CancellationToken>((u, _) => capturedUser = u)
            .Returns(Task.CompletedTask);

        usersRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        string? capturedBody = null;
        emailService
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, __, body, ___) => capturedBody = body)
            .Returns(Task.CompletedTask);

        var handler = new RegisterCommandHandler(
            usersRepo.Object,
            passwordService.Object,
            emailService.Object,
            TestHelpers.BuildConfiguration("https://frontend.example"),
            localizer.Object);

        //Act
        var cmd = new RegisterCommand { Name = "Andrei", Email = "Andrei@ye.com ", Password = "Parola123!" };
        var dto = await handler.Handle(cmd, CancellationToken.None);

        //Assert
        Assert.NotNull(dto);

        Assert.NotNull(capturedUser);
        Assert.Equal("andrei@ye.com", capturedUser!.Email);
        Assert.Equal("hashed", capturedUser.PasswordHash);
        Assert.False(capturedUser.EmailConfirmed);
        Assert.False(string.IsNullOrWhiteSpace(capturedUser.EmailConfirmationToken));
        Assert.NotNull(capturedUser.EmailConfirmationTokenExpiresAt);

        usersRepo.Verify(r => r.AddAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        usersRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        emailService.Verify(s => s.SendAsync(
            "andrei@ye.com",
            TestHelpers.ResxHelper["Register_Email_Confirmation_Subject"],
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);

        Assert.NotNull(capturedBody);
        Assert.Contains("https://frontend.example/verify-email?userId=", capturedBody);
        Assert.Contains("&token=", capturedBody);

        Assert.Equal(capturedUser.Id, dto!.Id);
        Assert.Equal("andrei@ye.com", dto.Email);
    }
}
