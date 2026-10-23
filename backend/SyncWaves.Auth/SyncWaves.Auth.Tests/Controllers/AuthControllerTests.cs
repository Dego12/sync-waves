using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SyncWaves.Auth.Application.Commands;
using SyncWaves.Auth.Application.DTOs;
using SyncWaves.Auth.Application.Queries;
using SyncWaves.Auth.Domain.Exceptions;
using System.Security.Authentication;
using System.Security.Claims;

namespace SyncWaves.Auth.UnitTests.Controllers;

public class AuthControllerTests
{
    [Fact]
    public async Task Register_WhenMediatorReturnsUser_ReturnsOkWithUser()
    {
        //Arrange
        var mediator = new Mock<IMediator>(MockBehavior.Strict);

        var cmd = new RegisterCommand { Name = "Andrei", Email = "andrei@ye.com", Password = "pass" };
        var dto = new UserDTO { Id = Guid.NewGuid(), Email = "andrei@ye.com" };

        mediator
            .Setup(m => m.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var (sut, _) = CreateSut(mediator);

        //Act
        var result = await sut.Register(cmd);

        //Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(dto, ok.Value);

        mediator.Verify(m => m.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Register_WhenMediatorThrowsArgumentException_PropagatesException()
    {
        //Arrange
        var mediator = new Mock<IMediator>(MockBehavior.Strict);

        var cmd = new RegisterCommand { Name = "Andrei", Email = "andrei@ye.com", Password = "pass" };

        mediator
            .Setup(m => m.Send(cmd, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Register_Email_Already_Registered"));

        var (sut, _) = CreateSut(mediator);

        //Act
        var act = () => sut.Register(cmd);

        //Assert
        await Assert.ThrowsAsync<ArgumentException>(act);

        mediator.Verify(m => m.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Register_WhenMediatorThrowsUnexpectedException_PropagatesException()
    {
        //Arrange
        var mediator = new Mock<IMediator>(MockBehavior.Strict);

        var cmd = new RegisterCommand { Name = "Andrei", Email = "andrei@ye.com", Password = "pass" };

        mediator
            .Setup(m => m.Send(cmd, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Boom"));

        var (sut, _) = CreateSut(mediator);

        //Act
        var act = () => sut.Register(cmd);

        //Assert
        await Assert.ThrowsAsync<Exception>(act);

        mediator.Verify(m => m.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_WhenMediatorReturnsLoginDto_ReturnsOkWithDto_AndSetsRefreshCookie()
    {
        //Arrange
        var mediator = new Mock<IMediator>(MockBehavior.Strict);

        var cmd = new LoginCommand { Email = "andrei@ye.com", Password = "pass" };
        var dto = new LoginDTO
        {
            AccessToken = "jwt",
            RefreshToken = "refresh-token",
            AccessTokenExpiresAt = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Email = "andrei@ye.com",
            UserId = Guid.NewGuid()
        };

        mediator
            .Setup(m => m.Send(It.Is<LoginCommand>(c => c.Email == cmd.Email && c.Password == cmd.Password), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var (sut, http) = CreateSut(mediator);

        //Act
        var result = await sut.Login(cmd);

        //Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(dto, ok.Value);

        var setCookie = http.Response.Headers["Set-Cookie"].ToString();
        Assert.Contains("syncwaves_refresh=", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("syncwaves_refresh=refresh-token", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", setCookie, StringComparison.OrdinalIgnoreCase);

        mediator.Verify(m => m.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_WhenMediatorReturnsNull_ReturnsUnauthorized_AndDoesNotSetCookie()
    {
        //Arrange
        var mediator = new Mock<IMediator>(MockBehavior.Strict);

        var cmd = new LoginCommand { Email = "andrei@ye.com", Password = "pass" };

        mediator
            .Setup(m => m.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoginDTO?)null);

        var (sut, http) = CreateSut(mediator);

        //Act
        var result = await sut.Login(cmd);

        //Assert
        Assert.IsType<UnauthorizedResult>(result);
        Assert.False(http.Response.Headers.ContainsKey("Set-Cookie"));

        mediator.Verify(m => m.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_WhenEmailNotConfirmedException_PropagatesException()
    {
        //Arrange
        var mediator = new Mock<IMediator>(MockBehavior.Strict);

        var cmd = new LoginCommand { Email = "andrei@ye.com", Password = "pass" };

        mediator
            .Setup(m => m.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EmailNotConfirmedException("Login_Email_Not_Confirmed"));

        var (sut, _) = CreateSut(mediator);

        //Act
        var act = () => sut.Login(cmd);

        //Assert
        await Assert.ThrowsAsync<EmailNotConfirmedException>(act);
    }

    [Fact]
    public async Task Login_WhenInvalidCredentialsException_PropagatesException()
    {
        //Arrange
        var mediator = new Mock<IMediator>(MockBehavior.Strict);

        var cmd = new LoginCommand { Email = "andrei@ye.com", Password = "pass" };

        mediator
            .Setup(m => m.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidCredentialException("Login_Invalid_Credentials"));

        var (sut, _) = CreateSut(mediator);

        //Act
        var act = () => sut.Login(cmd);

        //Assert
        await Assert.ThrowsAsync<InvalidCredentialException>(act);
    }

    [Fact]
    public async Task Refresh_WhenCookieMissing_ReturnsUnauthorized_AndDoesNotCallMediator()
    {
        //Arrange
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        var (sut, http) = CreateSut(mediator);

        //Act
        var result = await sut.Refresh();

        //Assert
        Assert.IsType<UnauthorizedResult>(result);
        Assert.False(http.Response.Headers.ContainsKey("Set-Cookie"));
    }

    [Fact]
    public async Task Refresh_WhenCookiePresent_ReturnsOkWithAccessToken_AndRotatesRefreshCookie()
    {
        //Arrange
        var mediator = new Mock<IMediator>(MockBehavior.Strict);

        var refreshCookie = "cookie-refresh";
        var handlerResult = new LoginDTO
        {
            AccessToken = "new-jwt",
            RefreshToken = "new-refresh",
            AccessTokenExpiresAt = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Email = "andrei@ye.com",
            UserId = Guid.NewGuid()
        };

        mediator
            .Setup(m => m.Send(It.Is<RefreshTokenCommand>(c => c.RawRefreshToken == refreshCookie), It.IsAny<CancellationToken>()))
            .ReturnsAsync(handlerResult);

        var (sut, http) = CreateSut(mediator);
        http.Request.Headers["Cookie"] = $"syncwaves_refresh={refreshCookie}";

        //Act
        var result = await sut.Refresh();

        //Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<LoginDTO>(ok.Value);
        Assert.Equal("new-jwt", body.AccessToken);

        var setCookie = http.Response.Headers["Set-Cookie"].ToString();
        Assert.Contains("syncwaves_refresh=new-refresh", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=none", setCookie, StringComparison.OrdinalIgnoreCase);

        mediator.Verify(m => m.Send(It.IsAny<RefreshTokenCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmEmail_WhenMediatorSucceeds_ReturnsOk()
    {
        //Arrange
        var mediator = new Mock<IMediator>(MockBehavior.Strict);

        var cmd = new ConfirmEmailCommand { UserId = Guid.NewGuid(), Token = "t" };

        mediator
            .Setup(m => m.Send(cmd, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var (sut, _) = CreateSut(mediator);

        //Act
        var result = await sut.ConfirmEmail(cmd);

        //Assert
        Assert.IsType<OkResult>(result);

        mediator.Verify(m => m.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmEmail_WhenAlreadyConfirmedException_PropagatesException()
    {
        //Arrange
        var mediator = new Mock<IMediator>(MockBehavior.Strict);

        var cmd = new ConfirmEmailCommand { UserId = Guid.NewGuid(), Token = "t" };

        mediator
            .Setup(m => m.Send(cmd, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EmailAlreadyConfirmedException("Confirm_Email_Already_Confirmed"));

        var (sut, _) = CreateSut(mediator);

        //Act
        var act = () => sut.ConfirmEmail(cmd);

        //Assert
        await Assert.ThrowsAsync<EmailAlreadyConfirmedException>(act);
    }

    [Fact]
    public async Task ForgotPassword_WhenMediatorSucceeds_ReturnsOk()
    {
        //Arrange
        var mediator = new Mock<IMediator>(MockBehavior.Strict);

        var cmd = new ForgotPasswordCommand { Email = "andrei@ye.com" };

        mediator
            .Setup(m => m.Send(cmd, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var (sut, _) = CreateSut(mediator);

        //Act
        var result = await sut.ForgotPassword(cmd);

        //Assert
        Assert.IsType<OkResult>(result);

        mediator.Verify(m => m.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_WhenUserNotFoundException_PropagatesException()
    {
        //Arrange
        var mediator = new Mock<IMediator>(MockBehavior.Strict);

        var cmd = new ForgotPasswordCommand { Email = "missing@ye.com" };

        mediator
            .Setup(m => m.Send(cmd, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UserNotFoundException("User_Does_Not_Exist"));

        var (sut, _) = CreateSut(mediator);

        //Act
        var act = () => sut.ForgotPassword(cmd);

        //Assert
        await Assert.ThrowsAsync<UserNotFoundException>(act);
    }

    [Fact]
    public async Task ResetPassword_WhenMediatorSucceeds_ReturnsOk()
    {
        //Arrange
        var mediator = new Mock<IMediator>(MockBehavior.Strict);

        var cmd = new ResetPasswordCommand { UserId = Guid.NewGuid(), Password = "new", Token = "t" };

        mediator
            .Setup(m => m.Send(cmd, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var (sut, _) = CreateSut(mediator);

        //Act
        var result = await sut.ResetPassword(cmd);

        //Assert
        Assert.IsType<OkResult>(result);

        mediator.Verify(m => m.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadProfileImage_WhenCalled_SendsUpdateUserCommandWithClaimUserId_AndReturnsOk()
    {
        //Arrange
        var mediator = new Mock<IMediator>(MockBehavior.Strict);

        var userId = Guid.NewGuid();
        var file = new FormFile(new MemoryStream(new byte[] { 1, 2, 3 }), 0, 3, "file", "pic.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        var dto = new UserDTO { Id = userId, Email = "andrei@ye.com", ProfileImageUrl = "https://cdn/pic.png" };

        mediator
            .Setup(m => m.Send(It.Is<UpdateUserCommand>(c => c.UserId == userId && ReferenceEquals(c.FormFile, file)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var (sut, http) = CreateSut(mediator, CreateUser(userId));
        http.User = CreateUser(userId);

        //Act
        var result = await sut.UploadProfileImage(file);

        //Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(dto, ok.Value);

        mediator.Verify(m => m.Send(It.IsAny<UpdateUserCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetLoggedUser_WhenCalled_SendsQueryWithClaimUserId_AndReturnsOk()
    {
        //Arrange
        var mediator = new Mock<IMediator>(MockBehavior.Strict);

        var userId = Guid.NewGuid();
        var dto = new UserDTO { Id = userId, Email = "andrei@ye.com", ProfileImageUrl = "https://cdn/pic.png" };

        mediator
            .Setup(m => m.Send(It.Is<GetLoggedUserQuery>(q => q.UserId == userId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var (sut, http) = CreateSut(mediator, CreateUser(userId));
        http.User = CreateUser(userId);

        //Act
        var result = await sut.GetLoggedUser();

        //Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(dto, ok.Value);

        mediator.Verify(m => m.Send(It.IsAny<GetLoggedUserQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginWithMicrosoft_WhenMediatorReturnsLoginDto_ReturnsOkWithDto_AndSetsRefreshCookie()
    {
        //Arrange
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        var cmd = new LoginWithMicrosoftCommand { IdToken = "id-token" };
        var dto = new LoginDTO
        {
            AccessToken = "jwt",
            RefreshToken = "refresh-hash",
            AccessTokenExpiresAt = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Email = "ms@ye.com",
            UserId = Guid.NewGuid()
        };

        mediator
            .Setup(m => m.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var (sut, http) = CreateSut(mediator);

        //Act
        var result = await sut.LoginWithMicrosoft(cmd);

        //Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(dto, ok.Value);

        var setCookie = http.Response.Headers["Set-Cookie"].ToString();
        Assert.Contains("syncwaves_refresh=refresh-hash", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=none", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("expires=", setCookie, StringComparison.OrdinalIgnoreCase);

        mediator.Verify(m => m.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }


    private static (AuthController sut, DefaultHttpContext httpContext) CreateSut(Mock<IMediator> mediator, ClaimsPrincipal? user = null)
    {
        var httpContext = new DefaultHttpContext
        {
            User = user ?? new ClaimsPrincipal(new ClaimsIdentity())
        };

        var sut = new AuthController(mediator.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        return (sut, httpContext);
    }

    private static ClaimsPrincipal CreateUser(Guid userId)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "test"));
    }
}
