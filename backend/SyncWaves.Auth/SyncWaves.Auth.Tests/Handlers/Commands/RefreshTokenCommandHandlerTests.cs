using Moq;
using SyncWaves.Auth.Application.Abstractions;
using SyncWaves.Auth.Application.Commands;
using SyncWaves.Auth.Application.DTOs;
using SyncWaves.Auth.Application.Handlers.Commands;
using SyncWaves.Auth.Domain.Entities;
using SyncWaves.Auth.Domain.Exceptions;

namespace SyncWaves.Auth.UnitTests.Handlers.Commands;

public class RefreshTokenCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenTokenNotFound_ThrowsTokenInvalidException()
    {
        //Arrange
        var refreshRepo = new Mock<IRefreshTokensRepository>(MockBehavior.Strict);
        var tokenGen = new Mock<IJwtTokenGenerator>(MockBehavior.Strict);
        var usersRepo = new Mock<IUsersRepository>(MockBehavior.Strict);

        refreshRepo
            .Setup(r => r.GetByTokenHashAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshTokenEntity?)null);

        var handler = new RefreshTokenCommandHandler(refreshRepo.Object, tokenGen.Object, usersRepo.Object);

        //Act
        var ex = await Assert.ThrowsAsync<TokenInvalidException>(() =>
            handler.Handle(new RefreshTokenCommand { RawRefreshToken = "missing" }, CancellationToken.None));

        //Assert
        Assert.Equal("Invalid_Token", ex.Message);

        usersRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        tokenGen.Verify(g => g.GenerateToken(It.IsAny<UserEntity>(), out It.Ref<DateTime>.IsAny), Times.Never);
        refreshRepo.Verify(r => r.AddAsync(It.IsAny<RefreshTokenEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        refreshRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTokenInactive_ThrowsTokenInvalidException()
    {
        //Arrange
        var refreshRepo = new Mock<IRefreshTokensRepository>(MockBehavior.Strict);
        var tokenGen = new Mock<IJwtTokenGenerator>(MockBehavior.Strict);
        var usersRepo = new Mock<IUsersRepository>(MockBehavior.Strict);

        var existing = new RefreshTokenEntity
        {
            UserId = Guid.NewGuid(),
            TokenHash = "hash",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        };

        refreshRepo
            .Setup(r => r.GetByTokenHashAsync("hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var handler = new RefreshTokenCommandHandler(refreshRepo.Object, tokenGen.Object, usersRepo.Object);

        //Act
        var ex = await Assert.ThrowsAsync<TokenInvalidException>(() =>
            handler.Handle(new RefreshTokenCommand { RawRefreshToken = "hash" }, CancellationToken.None));

        //Assert
        Assert.Equal("Invalid_Token", ex.Message);

        usersRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        refreshRepo.Verify(r => r.AddAsync(It.IsAny<RefreshTokenEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        refreshRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsUserNotFoundException()
    {
        //Arrange
        var refreshRepo = new Mock<IRefreshTokensRepository>(MockBehavior.Strict);
        var tokenGen = new Mock<IJwtTokenGenerator>(MockBehavior.Strict);
        var usersRepo = new Mock<IUsersRepository>(MockBehavior.Strict);

        var userId = Guid.NewGuid();
        var existing = new RefreshTokenEntity
        {
            UserId = userId,
            TokenHash = "hash",
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        refreshRepo
            .Setup(r => r.GetByTokenHashAsync("hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        usersRepo
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEntity?)null);

        var handler = new RefreshTokenCommandHandler(refreshRepo.Object, tokenGen.Object, usersRepo.Object);

        //Act
        var ex = await Assert.ThrowsAsync<UserNotFoundException>(() =>
            handler.Handle(new RefreshTokenCommand { RawRefreshToken = "hash" }, CancellationToken.None));

        //Assert
        Assert.Equal("User_Does_Not_Exist", ex.Message);

        tokenGen.Verify(g => g.GenerateToken(It.IsAny<UserEntity>(), out It.Ref<DateTime>.IsAny), Times.Never);
        refreshRepo.Verify(r => r.AddAsync(It.IsAny<RefreshTokenEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        refreshRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenValid_RevokesExisting_AddsNewRefreshToken_Saves_AndReturnsLoginDto()
    {
        //Arrange
        var refreshRepo = new Mock<IRefreshTokensRepository>(MockBehavior.Strict);
        var tokenGen = new Mock<IJwtTokenGenerator>(MockBehavior.Strict);
        var usersRepo = new Mock<IUsersRepository>(MockBehavior.Strict);

        var user = new UserEntity("andrei", "andrei@ye.com", "hash") { EmailConfirmed = true };

        var existing = new RefreshTokenEntity
        {
            UserId = user.Id,
            TokenHash = "old-hash",
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        refreshRepo
            .Setup(r => r.GetByTokenHashAsync("old-hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        usersRepo
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var expiresAt = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        tokenGen
            .Setup(g => g.GenerateToken(user, out expiresAt))
            .Returns("jwt");

        refreshRepo
            .Setup(r => r.HashToken(It.IsAny<string>()))
            .Returns("new-hash");

        RefreshTokenEntity? added = null;
        refreshRepo
            .Setup(r => r.AddAsync(It.IsAny<RefreshTokenEntity>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshTokenEntity, CancellationToken>((e, _) => added = e)
            .Returns(Task.CompletedTask);

        refreshRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new RefreshTokenCommandHandler(refreshRepo.Object, tokenGen.Object, usersRepo.Object);

        //Act
        var dto = await handler.Handle(new RefreshTokenCommand { RawRefreshToken = "old-hash" }, CancellationToken.None);

        //Assert
        Assert.NotNull(dto);
        Assert.Equal("jwt", dto.AccessToken);
        Assert.Equal("new-hash", dto.RefreshToken);
        Assert.Equal(user.Id, dto.UserId);
        Assert.Equal(user.Email, dto.Email);
        Assert.Equal(expiresAt, dto.AccessTokenExpiresAt);

        Assert.NotNull(existing.RevokedAt);
        Assert.Equal("new-hash", existing.ReplacedByTokenHash);

        Assert.NotNull(added);
        Assert.Equal(user.Id, added!.UserId);
        Assert.Equal("new-hash", added.TokenHash);

        refreshRepo.Verify(r => r.AddAsync(It.IsAny<RefreshTokenEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        refreshRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
