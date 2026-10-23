using Moq;
using SyncWaves.Auth.Application.Abstractions;
using SyncWaves.Auth.Application.Commands;
using SyncWaves.Auth.Application.DTOs;
using SyncWaves.Auth.Application.Handlers.Commands;
using SyncWaves.Auth.Domain.Abstractions;
using SyncWaves.Auth.Domain.Entities;
using System.Security.Authentication;
using System.Security.Claims;

namespace SyncWaves.Auth.UnitTests.Handlers;

public class LoginWithMicrosoftCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenPrincipalHasNoOidOrSub_ThrowsInvalidCredentialException()
    {
        //Arrange
        var tokenValidator = new Mock<IMicrosoftTokenValidator>(MockBehavior.Strict);
        var usersRepo = new Mock<IUsersRepository>(MockBehavior.Strict);
        var jwt = new Mock<IJwtTokenGenerator>(MockBehavior.Strict);
        var refreshRepo = new Mock<IRefreshTokensRepository>(MockBehavior.Strict);

        var principal = CreatePrincipal();

        tokenValidator
            .Setup(v => v.ValidateIdTokenAsync("id-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(principal);

        var sut = new LoginWithMicrosoftCommandHandler(
            tokenValidator.Object,
            usersRepo.Object,
            jwt.Object,
            refreshRepo.Object);

        //Act
        var act = () => sut.Handle(new LoginWithMicrosoftCommand { IdToken = "id-token" }, CancellationToken.None);

        //Assert
        var ex = await Assert.ThrowsAsync<InvalidCredentialException>(act);
        Assert.Equal("Invalid Microsoft token.", ex.Message);

        tokenValidator.Verify(v => v.ValidateIdTokenAsync("id-token", It.IsAny<CancellationToken>()), Times.Once);

        usersRepo.Verify(r => r.GetByExternalIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        usersRepo.Verify(r => r.AddAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        usersRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        jwt.Verify(g => g.GenerateToken(It.IsAny<UserEntity>(), out It.Ref<DateTime>.IsAny), Times.Never);

        refreshRepo.Verify(r => r.HashToken(It.IsAny<string>()), Times.Never);
        refreshRepo.Verify(r => r.AddAsync(It.IsAny<RefreshTokenEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        refreshRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserExists_DoesNotCreateUser_PersistsRefreshToken_AndReturnsLoginDto()
    {
        //Arrange
        var tokenValidator = new Mock<IMicrosoftTokenValidator>(MockBehavior.Strict);
        var usersRepo = new Mock<IUsersRepository>(MockBehavior.Strict);
        var jwt = new Mock<IJwtTokenGenerator>(MockBehavior.Strict);
        var refreshRepo = new Mock<IRefreshTokensRepository>(MockBehavior.Strict);

        var principal = CreatePrincipal(
            new Claim("oid", "ext-1"),
            new Claim(ClaimTypes.Email, "andrei@ye.com"));

        tokenValidator
            .Setup(v => v.ValidateIdTokenAsync("id-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(principal);

        var existingUser = new UserEntity("Andrei", "andrei@ye.com", "hash")
        {
            EmailConfirmed = true,
            ExternalProvider = "Microsoft",
            ExternalId = "ext-1"
        };

        usersRepo
            .Setup(r => r.GetByExternalIdAsync("Microsoft", "ext-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        var expiresAt = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        jwt
            .Setup(g => g.GenerateToken(existingUser, out expiresAt))
            .Returns("jwt");

        string? capturedRefreshRaw = null;
        refreshRepo
            .Setup(r => r.HashToken(It.IsAny<string>()))
            .Callback<string>(raw => capturedRefreshRaw = raw)
            .Returns("hashed-refresh");

        RefreshTokenEntity? savedRefresh = null;
        refreshRepo
            .Setup(r => r.AddAsync(It.IsAny<RefreshTokenEntity>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshTokenEntity, CancellationToken>((e, _) => savedRefresh = e)
            .Returns(Task.CompletedTask);

        refreshRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new LoginWithMicrosoftCommandHandler(
            tokenValidator.Object,
            usersRepo.Object,
            jwt.Object,
            refreshRepo.Object);

        //Act
        var dto = await sut.Handle(new LoginWithMicrosoftCommand { IdToken = "id-token" }, CancellationToken.None);

        //Assert
        Assert.NotNull(dto);
        Assert.Equal("jwt", dto.AccessToken);
        Assert.Equal("hashed-refresh", dto.RefreshToken);
        Assert.Equal(existingUser.Email, dto.Email);
        Assert.Equal(existingUser.Id, dto.UserId);
        Assert.Equal(expiresAt, dto.AccessTokenExpiresAt);

        Assert.NotNull(capturedRefreshRaw);
        var bytes = Convert.FromBase64String(capturedRefreshRaw!);
        Assert.Equal(64, bytes.Length);

        Assert.NotNull(savedRefresh);
        Assert.Equal(existingUser.Id, savedRefresh!.UserId);
        Assert.Equal("hashed-refresh", savedRefresh.TokenHash);
        Assert.Equal(expiresAt, savedRefresh.ExpiresAt);

        tokenValidator.Verify(v => v.ValidateIdTokenAsync("id-token", It.IsAny<CancellationToken>()), Times.Once);
        usersRepo.Verify(r => r.GetByExternalIdAsync("Microsoft", "ext-1", It.IsAny<CancellationToken>()), Times.Once);

        usersRepo.Verify(r => r.AddAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        usersRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        jwt.Verify(g => g.GenerateToken(existingUser, out expiresAt), Times.Once);

        refreshRepo.Verify(r => r.HashToken(It.IsAny<string>()), Times.Once);
        refreshRepo.Verify(r => r.AddAsync(It.IsAny<RefreshTokenEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        refreshRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_CreatesUser_PersistsUser_PersistsRefreshToken_AndReturnsLoginDto()
    {
        //Arrange
        var tokenValidator = new Mock<IMicrosoftTokenValidator>(MockBehavior.Strict);
        var usersRepo = new Mock<IUsersRepository>(MockBehavior.Strict);
        var jwt = new Mock<IJwtTokenGenerator>(MockBehavior.Strict);
        var refreshRepo = new Mock<IRefreshTokensRepository>(MockBehavior.Strict);

        var principal = CreatePrincipal(
            new Claim("oid", "ext-2"),
            new Claim(ClaimTypes.Email, "new@ye.com"));

        tokenValidator
            .Setup(v => v.ValidateIdTokenAsync("id-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(principal);

        usersRepo
            .Setup(r => r.GetByExternalIdAsync("Microsoft", "ext-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEntity?)null);

        UserEntity? createdUser = null;
        usersRepo
            .Setup(r => r.AddAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()))
            .Callback<UserEntity, CancellationToken>((u, _) => createdUser = u)
            .Returns(Task.CompletedTask);

        usersRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var expiresAt = new DateTime(2031, 2, 3, 4, 5, 6, DateTimeKind.Utc);
        jwt
            .Setup(g => g.GenerateToken(It.IsAny<UserEntity>(), out expiresAt))
            .Returns("jwt-new");

        refreshRepo
            .Setup(r => r.HashToken(It.IsAny<string>()))
            .Returns("hashed-refresh-new");

        RefreshTokenEntity? savedRefresh = null;
        refreshRepo
            .Setup(r => r.AddAsync(It.IsAny<RefreshTokenEntity>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshTokenEntity, CancellationToken>((e, _) => savedRefresh = e)
            .Returns(Task.CompletedTask);

        refreshRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new LoginWithMicrosoftCommandHandler(
            tokenValidator.Object,
            usersRepo.Object,
            jwt.Object,
            refreshRepo.Object);

        //Act
        var dto = await sut.Handle(new LoginWithMicrosoftCommand { IdToken = "id-token" }, CancellationToken.None);

        //Assert
        Assert.NotNull(createdUser);
        Assert.Equal("Microsoft", createdUser!.ExternalProvider);
        Assert.Equal("ext-2", createdUser.ExternalId);
        Assert.Equal("new@ye.com", createdUser.Email);
        Assert.True(createdUser.EmailConfirmed);
        Assert.Equal(string.Empty, createdUser.PasswordHash);
        Assert.NotEqual(default, createdUser.CreatedAt);

        Assert.NotNull(savedRefresh);
        Assert.Equal(createdUser.Id, savedRefresh!.UserId);
        Assert.Equal("hashed-refresh-new", savedRefresh.TokenHash);
        Assert.Equal(expiresAt, savedRefresh.ExpiresAt);

        Assert.NotNull(dto);
        Assert.Equal("jwt-new", dto.AccessToken);
        Assert.Equal("hashed-refresh-new", dto.RefreshToken);
        Assert.Equal(createdUser.Email, dto.Email);
        Assert.Equal(createdUser.Id, dto.UserId);
        Assert.Equal(expiresAt, dto.AccessTokenExpiresAt);

        tokenValidator.Verify(v => v.ValidateIdTokenAsync("id-token", It.IsAny<CancellationToken>()), Times.Once);
        usersRepo.Verify(r => r.GetByExternalIdAsync("Microsoft", "ext-2", It.IsAny<CancellationToken>()), Times.Once);

        usersRepo.Verify(r => r.AddAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        usersRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        jwt.Verify(g => g.GenerateToken(It.Is<UserEntity>(u => ReferenceEquals(u, createdUser)), out It.Ref<DateTime>.IsAny), Times.Once);

        refreshRepo.Verify(r => r.HashToken(It.IsAny<string>()), Times.Once);
        refreshRepo.Verify(r => r.AddAsync(It.IsAny<RefreshTokenEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        refreshRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailClaimMissing_UsesFallbackUnknownMicrosoftEmail()
    {
        //Arrange
        var tokenValidator = new Mock<IMicrosoftTokenValidator>(MockBehavior.Strict);
        var usersRepo = new Mock<IUsersRepository>(MockBehavior.Strict);
        var jwt = new Mock<IJwtTokenGenerator>(MockBehavior.Strict);
        var refreshRepo = new Mock<IRefreshTokensRepository>(MockBehavior.Strict);

        var principal = CreatePrincipal(new Claim("oid", "ext-3"));

        tokenValidator
            .Setup(v => v.ValidateIdTokenAsync("id-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(principal);

        usersRepo
            .Setup(r => r.GetByExternalIdAsync("Microsoft", "ext-3", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEntity?)null);

        UserEntity? createdUser = null;
        usersRepo
            .Setup(r => r.AddAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()))
            .Callback<UserEntity, CancellationToken>((u, _) => createdUser = u)
            .Returns(Task.CompletedTask);

        usersRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var expiresAt = new DateTime(2032, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        jwt
            .Setup(g => g.GenerateToken(It.IsAny<UserEntity>(), out expiresAt))
            .Returns("jwt");

        refreshRepo
            .Setup(r => r.HashToken(It.IsAny<string>()))
            .Returns("hashed-refresh");

        refreshRepo
            .Setup(r => r.AddAsync(It.IsAny<RefreshTokenEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        refreshRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new LoginWithMicrosoftCommandHandler(
            tokenValidator.Object,
            usersRepo.Object,
            jwt.Object,
            refreshRepo.Object);

        //Act
        var dto = await sut.Handle(new LoginWithMicrosoftCommand { IdToken = "id-token" }, CancellationToken.None);

        //Assert
        Assert.NotNull(createdUser);
        Assert.EndsWith("@unknown.microsoft", createdUser!.Email, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ext-3", createdUser.Email, StringComparison.OrdinalIgnoreCase);

        Assert.NotNull(dto);
        Assert.Equal(createdUser.Email, dto.Email);
    }

    [Fact]
    public async Task Handle_WhenOidIsMissing_UsesSubAsExternalId()
    {
        //Arrange
        var tokenValidator = new Mock<IMicrosoftTokenValidator>(MockBehavior.Strict);
        var usersRepo = new Mock<IUsersRepository>(MockBehavior.Strict);
        var jwt = new Mock<IJwtTokenGenerator>(MockBehavior.Strict);
        var refreshRepo = new Mock<IRefreshTokensRepository>(MockBehavior.Strict);

        var principal = CreatePrincipal(
            new Claim("sub", "sub-1"),
            new Claim("preferred_username", "preferred@ye.com"));

        tokenValidator
            .Setup(v => v.ValidateIdTokenAsync("id-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(principal);

        usersRepo
            .Setup(r => r.GetByExternalIdAsync("Microsoft", "sub-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEntity?)null);

        UserEntity? createdUser = null;
        usersRepo
            .Setup(r => r.AddAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()))
            .Callback<UserEntity, CancellationToken>((u, _) => createdUser = u)
            .Returns(Task.CompletedTask);

        usersRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var expiresAt = new DateTime(2033, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        jwt
            .Setup(g => g.GenerateToken(It.IsAny<UserEntity>(), out expiresAt))
            .Returns("jwt");

        refreshRepo
            .Setup(r => r.HashToken(It.IsAny<string>()))
            .Returns("hashed-refresh");

        refreshRepo
            .Setup(r => r.AddAsync(It.IsAny<RefreshTokenEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        refreshRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new LoginWithMicrosoftCommandHandler(
            tokenValidator.Object,
            usersRepo.Object,
            jwt.Object,
            refreshRepo.Object);

        //Act
        await sut.Handle(new LoginWithMicrosoftCommand { IdToken = "id-token" }, CancellationToken.None);

        //Assert
        Assert.NotNull(createdUser);
        Assert.Equal("Microsoft", createdUser!.ExternalProvider);
        Assert.Equal("sub-1", createdUser.ExternalId);
        Assert.Equal("preferred@ye.com", createdUser.Email);

        usersRepo.Verify(r => r.GetByExternalIdAsync("Microsoft", "sub-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims)
        => new(new ClaimsIdentity(claims, "test"));
}
