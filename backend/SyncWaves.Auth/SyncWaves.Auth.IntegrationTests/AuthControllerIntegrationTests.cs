using SyncWaves.Auth.Application.Commands;
using SyncWaves.Auth.Application.DTOs;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace SyncWaves.Auth.IntegrationTests;

public class AuthControllerIntegrationTests : IClassFixture<AuthApiFixture>
{
    private readonly AuthApiFixture _fx;

    public AuthControllerIntegrationTests(AuthApiFixture fx)
    {
        _fx = fx;
        _fx.Users.Clear();
        _fx.Emails.Clear();
        _fx.RefreshTokens.Clear();
        _fx.MicrosoftTokens.Clear();
    }

    [Fact]
    public async Task Register_WhenValid_ReturnsOk_AndSendsVerificationEmail()
    {
        //Arrange
        var register = new RegisterCommand
        {
            Name = "Andrei",
            Email = "andrei@ye.com",
            Password = "Parola123!"
        };

        //Act
        var resp = await _fx.Client.PostAsJsonAsync("/api/auth/register", register);

        //Assert
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("andrei@ye.com", body);

        Assert.Single(_fx.Emails.Sent);

        var mail = _fx.Emails.Sent[0];
        Assert.Equal("andrei@ye.com", mail.To);
        Assert.Contains("/verify-email?userId=", mail.HtmlBody);
        Assert.Contains("&token=", mail.HtmlBody);
    }

    [Fact]
    public async Task Register_WhenDuplicateEmail_ReturnsConflict()
    {
        //Arrange
        var register = new RegisterCommand
        {
            Name = "Andrei",
            Email = "andrei@ye.com",
            Password = "Parola123!"
        };

        //Act
        var r1 = await _fx.Client.PostAsJsonAsync("/api/auth/register", register);
        var r2 = await _fx.Client.PostAsJsonAsync("/api/auth/register", register);

        //Assert
        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, r2.StatusCode);
    }

    [Fact]
    public async Task Login_WhenEmailNotConfirmed_ReturnsBadRequest()
    {
        //Arrange
        var register = new RegisterCommand
        {
            Name = "Andrei",
            Email = "andrei@ye.com",
            Password = "Parola123!"
        };

        var r = await _fx.Client.PostAsJsonAsync("/api/auth/register", register);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);

        var login = new LoginCommand { Email = "andrei@ye.com", Password = "Parola123!" };

        //Act
        var l = await _fx.Client.PostAsJsonAsync("/api/auth/login", login);

        //Assert
        Assert.Equal(HttpStatusCode.BadRequest, l.StatusCode);

        var msg = await l.Content.ReadAsStringAsync();
        Assert.Contains("Email not confirmed", msg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConfirmEmail_WhenConfirmed_AllowsLogin()
    {
        //Arrange
        await RegisterAndConfirm("andrei@ye.com", "Parola123!");

        var login = new LoginCommand { Email = "andrei@ye.com", Password = "Parola123!", RememberMe = false };

        //Act
        var l = await _fx.Client.PostAsJsonAsync("/api/auth/login", login);

        //Assert
        Assert.Equal(HttpStatusCode.OK, l.StatusCode);

        var loginBody = await l.Content.ReadAsStringAsync();
        Assert.Contains("accessToken", loginBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ForgotAndResetPassword_WhenCompleted_ThenLoginWithNewPassword_Succeeds()
    {
        //Arrange
        await RegisterAndConfirm("andrei@ye.com", "Parola123!");
        _fx.Emails.Clear();

        var fp = new ForgotPasswordCommand { Email = "andrei@ye.com" };

        //Act
        var fpResp = await _fx.Client.PutAsJsonAsync("/api/auth/forgot-password", fp);

        //Assert
        Assert.Equal(HttpStatusCode.OK, fpResp.StatusCode);

        Assert.Single(_fx.Emails.Sent);
        var resetUrl = ExtractFirstUrl(_fx.Emails.Sent.Single().HtmlBody);
        var (userId, token) = ParseUserIdAndToken(resetUrl);

        var rp = new ResetPasswordCommand { UserId = userId, Token = token, Password = "ParolaNoua123!" };
        var rpResp = await _fx.Client.PutAsJsonAsync("/api/auth/reset-password", rp);
        Assert.Equal(HttpStatusCode.OK, rpResp.StatusCode);

        var login = new LoginCommand { Email = "andrei@ye.com", Password = "ParolaNoua123!" };
        var l = await _fx.Client.PostAsJsonAsync("/api/auth/login", login);

        Assert.Equal(HttpStatusCode.OK, l.StatusCode);
    }

    [Fact]
    public async Task Refresh_WhenCookieIsPresent_ReturnsOk_AndRotatesRefreshCookie()
    {
        //Arrange
        await RegisterAndConfirm("andrei@ye.com", "Parola123!");

        var login = new LoginCommand { Email = "andrei@ye.com", Password = "Parola123!" };
        var loginResp = await _fx.Client.PostAsJsonAsync("/api/auth/login", login);
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);

        var refreshCookie = ExtractCookieValue(loginResp, "syncwaves_refresh");
        Assert.False(string.IsNullOrWhiteSpace(refreshCookie));

        using var refreshReq = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refreshReq.Headers.Add("Cookie", $"syncwaves_refresh={refreshCookie}");

        //Act
        var refreshResp = await _fx.Client.SendAsync(refreshReq);

        //Assert
        Assert.Equal(HttpStatusCode.OK, refreshResp.StatusCode);

        var json = await refreshResp.Content.ReadAsStringAsync();
        Assert.Contains("accessToken", json, StringComparison.OrdinalIgnoreCase);

        var rotated = ExtractCookieValue(refreshResp, "syncwaves_refresh");
        Assert.False(string.IsNullOrWhiteSpace(rotated));
        Assert.NotEqual(refreshCookie, rotated);
    }

    private async Task RegisterAndConfirm(string email, string password)
    {
        _fx.Emails.Clear();

        var register = new RegisterCommand
        {
            Name = "Andrei",
            Email = email,
            Password = password
        };

        var r = await _fx.Client.PostAsJsonAsync("/api/auth/register", register);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);

        var verifyUrl = ExtractFirstUrl(_fx.Emails.Sent.Single().HtmlBody);
        var (userId, token) = ParseUserIdAndToken(verifyUrl);

        var confirm = new ConfirmEmailCommand { UserId = userId, Token = token };
        var c = await _fx.Client.PutAsJsonAsync("/api/auth/confirm-email", confirm);
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
    }

    [Fact]
    public async Task LoginWithMicrosoft_WhenValid_ReturnsOk_SetsRefreshCookie_AndCreatesUser()
    {
        //Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("oid", "oid-123"),
            new Claim(ClaimTypes.Email, "ms@ye.com"),
            new Claim("name", "Microsoft User")
        }, "ms"));

        _fx.MicrosoftTokens.SetPrincipal("ms-token", principal);

        var cmd = new LoginWithMicrosoftCommand { IdToken = "ms-token" };

        //Act
        var resp = await _fx.Client.PostAsJsonAsync("/api/auth/login/microsoft", cmd);

        //Assert
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var dto = await resp.Content.ReadFromJsonAsync<LoginDTO>();
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(dto.RefreshToken));
        Assert.Equal("ms@ye.com", dto.Email);

        var setCookie = GetSetCookieHeader(resp);
        Assert.Contains("syncwaves_refresh=", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=none", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("expires=", setCookie, StringComparison.OrdinalIgnoreCase);

        var user = await _fx.Users.GetByEmailAsync("ms@ye.com");
        Assert.NotNull(user);

        Assert.Single(_fx.RefreshTokens.Added);
        var saved = _fx.RefreshTokens.Added.Single();
        Assert.Equal(user!.Id, saved.UserId);
    }

    [Fact]
    public async Task LoginWithMicrosoft_WhenCalledTwiceForSameExternalId_DoesNotCreateDuplicateUser()
    {
        //Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("oid", "oid-999"),
            new Claim(ClaimTypes.Email, "ms@ye.com"),
            new Claim("name", "Microsoft User")
        }, "ms"));  

        _fx.MicrosoftTokens.SetPrincipal("ms-token", principal);

        var cmd = new LoginWithMicrosoftCommand { IdToken = "ms-token" };

        //Act
        var r1 = await _fx.Client.PostAsJsonAsync("/api/auth/login/microsoft", cmd);
        var r2 = await _fx.Client.PostAsJsonAsync("/api/auth/login/microsoft", cmd);

        //Assert
        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);

        var d1 = await r1.Content.ReadFromJsonAsync<LoginDTO>();
        var d2 = await r2.Content.ReadFromJsonAsync<LoginDTO>();
        Assert.NotNull(d1);
        Assert.NotNull(d2);

        Assert.Equal(d1!.UserId, d2!.UserId);
        Assert.Single(_fx.Users.Added);
        Assert.Equal(2, _fx.RefreshTokens.Added.Count);
    }

    private static string ExtractFirstUrl(string html)
    {
        var m = Regex.Match(html, "href=\\\"(?<u>[^\\\"]+)\\\"", RegexOptions.IgnoreCase);
        if (!m.Success) throw new InvalidOperationException("Could not find href in email body. Body: " + html);
        return m.Groups["u"].Value;
    }

    private static (Guid userId, string token) ParseUserIdAndToken(string url)
    {
        var uri = new Uri(url);
        var q = System.Web.HttpUtility.ParseQueryString(uri.Query);

        var userId = Guid.Parse(q["userId"] ?? throw new InvalidOperationException("Missing userId in url: " + url));
        var token = q["token"] ?? throw new InvalidOperationException("Missing token in url: " + url);

        return (userId, token);
    }

    private static string GetSetCookieHeader(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
            return string.Empty;

        return string.Join("\n", values);
    }

    private static string ExtractCookieValue(HttpResponseMessage response, string cookieName)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
            return string.Empty;

        foreach (var header in values)
        {
            var parts = header.Split(';', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
                continue;

            if (!parts[0].StartsWith(cookieName + "=", StringComparison.OrdinalIgnoreCase))
                continue;

            return parts[0].Substring((cookieName + "=").Length);
        }

        return string.Empty;
    }
}
