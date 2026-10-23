using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncWaves.Auth.Application.Commands;
using SyncWaves.Auth.Application.DTOs;
using SyncWaves.Auth.Application.Queries;
using SyncWaves.Auth.Domain.Exceptions;
using System.Security.Authentication;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterCommand registerCommand)
    {
        var result = await _mediator.Send(registerCommand);

        return Ok(result);

    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginCommand loginCommand)
    {
        var result = await _mediator.Send(loginCommand);
        
        if (result == null)
        {
            return Unauthorized();
        }

        Response.Cookies.Append("syncwaves_refresh", result.RefreshToken, new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddDays(7)
        });

        return Ok(result);
    }

    [HttpPost("login/microsoft")]
    public async Task<IActionResult> LoginWithMicrosoft([FromBody] LoginWithMicrosoftCommand command)
    {
        var result = await _mediator.Send(command);

        Response.Cookies.Append("syncwaves_refresh", result.RefreshToken, new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddDays(7)
        });

        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies["syncwaves_refresh"];
        if(string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized();
        }

        var command = new RefreshTokenCommand { RawRefreshToken = refreshToken };

        var result = await _mediator.Send(command);

        Response.Cookies.Append("syncwaves_refresh", result.RefreshToken, new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = result.AccessTokenExpiresAt.AddDays(7)
        });

        return Ok(new LoginDTO { AccessToken = result.AccessToken });
    }

    [HttpPut("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailCommand confirmEmailCommand)
    {
        await _mediator.Send(confirmEmailCommand);

        return Ok();
    }

    [HttpPut("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordCommand forgotPasswordCommand)
    {
        await _mediator.Send(forgotPasswordCommand);

        return Ok();
    }

    [HttpPut("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordCommand resetPasswordCommand)
    {
        await _mediator.Send(resetPasswordCommand);

        return Ok();
    }

    [Authorize]
    [HttpPut("profile-image")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadProfileImage([FromForm] IFormFile file)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var command = new UpdateUserCommand { FormFile = file, UserId = Guid.Parse(userId) };
        var dto = await _mediator.Send(command);

        return Ok(dto);
    }

    [Authorize]
    [HttpGet("logged")]
    public async Task<IActionResult> GetLoggedUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var query = new GetLoggedUserQuery() { UserId = Guid.Parse(userId) };
        var dto = await _mediator.Send(query);

        return Ok(dto);
    }
}
