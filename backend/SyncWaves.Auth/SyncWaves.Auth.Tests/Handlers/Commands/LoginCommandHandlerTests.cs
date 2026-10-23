using Microsoft.AspNetCore.Http;
using Moq;
using SyncWaves.Auth.Application.Abstractions;
using SyncWaves.Auth.Application.Commands;
using SyncWaves.Auth.Application.Handlers.Commands;
using SyncWaves.Auth.Domain.Entities;
using SyncWaves.Auth.Domain.Exceptions;
using System.Security.Authentication;

namespace SyncWaves.Auth.UnitTests.Handlers.Commands;

public class LoginCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsUserNotFoundException()
    {
        //Arrange
        var usersRepo = new Mock<IUsersRepository>(MockBehavior.Strict);
        var passwordService = new Mock<IPasswordService>(MockBehavior.Strict);
        var tokenGen = new Mock<IJwtTokenGenerator>(MockBehavior.Strict);
        var refreshRepo = new Mock<IRefreshTokensRepository>(MockBehavior.Strict);
        var httpContextAccessor = new Mock<IHttpContextAccessor>(MockBehavior.Strict);

        usersRepo
            .Setup(r => r.GetByEmailAsync("x@ye.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEntity?)null);

        var handler = new LoginCommandHandler(
            usersRepo.Object,
            passwordService.Object,
            tokenGen.Object,
            refreshRepo.Object,
            httpContextAccessor.Object);

        //Act
        var ex = await Assert.ThrowsAsync<UserNotFoundException>(() =>
            handler.Handle(new LoginCommand { Email = "x@ye.com", Password = "pw" }, CancellationToken.None));

        //Assert
        Assert.Equal("Login_Invalid_Credentials", ex.Message);

        usersRepo.Verify(r => r.GetByEmailAsync("x@ye.com", It.IsAny<CancellationToken>()), Times.Once);
        passwordService.Verify(p => p.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        tokenGen.Verify(g => g.GenerateToken(It.IsAny<UserEntity>(), out It.Ref<DateTime>.IsAny), Times.Never);
        refreshRepo.Verify(r => r.AddAsync(It.IsAny<RefreshTokenEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        refreshRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPasswordInvalid_ThrowsInvalidCredentialException()
    {
        //Arrange
        var usersRepo = new Mock<IUsersRepository>(MockBehavior.Strict);
        var passwordService = new Mock<IPasswordService>(MockBehavior.Strict);
        var tokenGen = new Mock<IJwtTokenGenerator>(MockBehavior.Strict);
        var refreshRepo = new Mock<IRefreshTokensRepository>(MockBehavior.Strict);
        var httpContextAccessor = new Mock<IHttpContextAccessor>(MockBehavior.Strict);

        var user = new UserEntity("andrei", "andrei@ye.com", "hash") { EmailConfirmed = true };

        usersRepo
            .Setup(r => r.GetByEmailAsync("andrei@ye.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        passwordService
            .Setup(p => p.Verify("wrong", "hash"))
            .Returns(false);

        var handler = new LoginCommandHandler(
            usersRepo.Object,
            passwordService.Object,
            tokenGen.Object,
            refreshRepo.Object,
            httpContextAccessor.Object);

        //Act
        var ex = await Assert.ThrowsAsync<InvalidCredentialException>(() =>
            handler.Handle(new LoginCommand { Email = "andrei@ye.com", Password = "wrong" }, CancellationToken.None));

        //Assert
        Assert.Equal("Login_Invalid_Credentials", ex.Message);

        usersRepo.Verify(r => r.GetByEmailAsync("andrei@ye.com", It.IsAny<CancellationToken>()), Times.Once);
        passwordService.Verify(p => p.Verify("wrong", "hash"), Times.Once);
        tokenGen.Verify(g => g.GenerateToken(It.IsAny<UserEntity>(), out It.Ref<DateTime>.IsAny), Times.Never);
        refreshRepo.Verify(r => r.AddAsync(It.IsAny<RefreshTokenEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        refreshRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEmailNotConfirmed_ThrowsEmailNotConfirmedException()
    {
        //Arrange
        var usersRepo = new Mock<IUsersRepository>(MockBehavior.Strict);
        var passwordService = new Mock<IPasswordService>(MockBehavior.Strict);
        var tokenGen = new Mock<IJwtTokenGenerator>(MockBehavior.Strict);
        var refreshRepo = new Mock<IRefreshTokensRepository>(MockBehavior.Strict);
        var httpContextAccessor = new Mock<IHttpContextAccessor>(MockBehavior.Strict);

        var user = new UserEntity("andrei", "andrei@ye.com", "hash") { EmailConfirmed = false };

        usersRepo
            .Setup(r => r.GetByEmailAsync("andrei@ye.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        passwordService
            .Setup(p => p.Verify("ok", "hash"))
            .Returns(true);

        var handler = new LoginCommandHandler(
            usersRepo.Object,
            passwordService.Object,
            tokenGen.Object,
            refreshRepo.Object,
            httpContextAccessor.Object);

        //Act
        var ex = await Assert.ThrowsAsync<EmailNotConfirmedException>(() =>
            handler.Handle(new LoginCommand { Email = "andrei@ye.com", Password = "ok" }, CancellationToken.None));

        //Assert
        Assert.Equal("Login_Email_Not_Confirmed", ex.Message);

        tokenGen.Verify(g => g.GenerateToken(It.IsAny<UserEntity>(), out It.Ref<DateTime>.IsAny), Times.Never);
        refreshRepo.Verify(r => r.AddAsync(It.IsAny<RefreshTokenEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        refreshRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenValid_RememberMeFalse_PersistsRefreshToken_AndReturnsLoginDto()
    {
        //Arrange
        var usersRepo = new Mock<IUsersRepository>(MockBehavior.Strict);
        var passwordService = new Mock<IPasswordService>(MockBehavior.Strict);
        var tokenGen = new Mock<IJwtTokenGenerator>(MockBehavior.Strict);
        var refreshRepo = new Mock<IRefreshTokensRepository>(MockBehavior.Strict);
        var httpContextAccessor = new Mock<IHttpContextAccessor>(MockBehavior.Strict);

        var user = new UserEntity("andrei", "andrei@ye.com", "hash") { EmailConfirmed = true };

        usersRepo
            .Setup(r => r.GetByEmailAsync("andrei@ye.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        passwordService
            .Setup(p => p.Verify("ok", "hash"))
            .Returns(true);

        var expiresAt = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        tokenGen
            .Setup(g => g.GenerateToken(user, out expiresAt))
            .Returns("jwt");

        refreshRepo
            .Setup(r => r.HashToken(It.IsAny<string>()))
            .Returns("hashed-refresh");

        RefreshTokenEntity? saved = null;
        refreshRepo
            .Setup(r => r.AddAsync(It.IsAny<RefreshTokenEntity>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshTokenEntity, CancellationToken>((e, _) => saved = e)
            .Returns(Task.CompletedTask);

        refreshRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new LoginCommandHandler(
            usersRepo.Object,
            passwordService.Object,
            tokenGen.Object,
            refreshRepo.Object,
            httpContextAccessor.Object);

        //Act
        var result = await handler.Handle(
            new LoginCommand { Email = "andrei@ye.com", Password = "ok", RememberMe = false },
            CancellationToken.None);

        //Assert
        Assert.NotNull(result);
        Assert.Equal("jwt", result!.AccessToken);
        Assert.Equal("hashed-refresh", result.RefreshToken);
        Assert.Equal(expiresAt, result.AccessTokenExpiresAt);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(user.Email, result.Email);

        Assert.NotNull(saved);
        Assert.Equal(user.Id, saved!.UserId);
        Assert.Equal("hashed-refresh", saved.TokenHash);
        Assert.Equal(expiresAt, saved.ExpiresAt);

        refreshRepo.Verify(r => r.AddAsync(It.IsAny<RefreshTokenEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        refreshRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenValid_RememberMeTrue_SetsRefreshExpiryToJwtExpiryPlus7Days()
    {
        //Arrange
        var usersRepo = new Mock<IUsersRepository>(MockBehavior.Strict);
        var passwordService = new Mock<IPasswordService>(MockBehavior.Strict);
        var tokenGen = new Mock<IJwtTokenGenerator>(MockBehavior.Strict);
        var refreshRepo = new Mock<IRefreshTokensRepository>(MockBehavior.Strict);
        var httpContextAccessor = new Mock<IHttpContextAccessor>(MockBehavior.Strict);

        var user = new UserEntity("andrei", "andrei@ye.com", "hash") { EmailConfirmed = true };

        usersRepo
            .Setup(r => r.GetByEmailAsync("andrei@ye.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        passwordService
            .Setup(p => p.Verify("ok", "hash"))
            .Returns(true);

        var expiresAt = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        tokenGen
            .Setup(g => g.GenerateToken(user, out expiresAt))
            .Returns("jwt");

        refreshRepo
            .Setup(r => r.HashToken(It.IsAny<string>()))
            .Returns("hashed-refresh");

        RefreshTokenEntity? saved = null;
        refreshRepo
            .Setup(r => r.AddAsync(It.IsAny<RefreshTokenEntity>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshTokenEntity, CancellationToken>((e, _) => saved = e)
            .Returns(Task.CompletedTask);

        refreshRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new LoginCommandHandler(
            usersRepo.Object,
            passwordService.Object,
            tokenGen.Object,
            refreshRepo.Object,
            httpContextAccessor.Object);

        //Act
        var result = await handler.Handle(
            new LoginCommand { Email = "andrei@ye.com", Password = "ok", RememberMe = true },
            CancellationToken.None);

        //Assert
        Assert.NotNull(saved);
        Assert.Equal(expiresAt.AddDays(7), saved!.ExpiresAt);

        Assert.NotNull(result);
        Assert.Equal("hashed-refresh", result!.RefreshToken);
    }
}
