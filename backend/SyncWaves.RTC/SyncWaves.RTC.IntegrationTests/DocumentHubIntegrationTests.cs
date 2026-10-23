using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.IdentityModel.Tokens;

namespace SyncWaves.RTC.IntegrationTests;

public class DocumentHubIntegrationTests : IClassFixture<RtcApiFactory>
{
    private readonly RtcApiFactory _factory;

    public DocumentHubIntegrationTests(RtcApiFactory factory)
    {
        _factory = factory;
    }

    private static string CreateJwt(string userId)
    {
        var keyBytes = Encoding.UTF8.GetBytes(RtcApiFactory.TestJwtKey);
        var key = new SymmetricSecurityKey(keyBytes);
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userId)
        };

        var token = new JwtSecurityToken(
            issuer: RtcApiFactory.TestJwtIssuer,
            audience: RtcApiFactory.TestJwtAudience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private HubConnection CreateConnection(string? userId = null)
    {
        var client = _factory.CreateClient();
        var url = new Uri(client.BaseAddress!, "/hubs/document");

        return new HubConnectionBuilder()
            .WithUrl(url, options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;

                if (userId is not null)
                    options.AccessTokenProvider = () => Task.FromResult(CreateJwt(userId))!;
            })
            .Build();
    }

    private static async Task WaitUntil(Func<bool> condition, int timeoutMs = 2000)
    {
        var start = DateTime.UtcNow;
        while (!condition())
        {
            if ((DateTime.UtcNow - start).TotalMilliseconds > timeoutMs)
                return;

            await Task.Delay(25);
        }
    }

    [Fact]
    public async Task Connect_WithoutToken_Fails()
    {
        //Arrange
        var connection = CreateConnection();

        //Act
        Func<Task> act = () => connection.StartAsync();

        //Assert
        await Assert.ThrowsAnyAsync<Exception>(act);
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task JoinDocument_SendsDocumentLoaded()
    {
        //Arrange
        var documentId = Guid.NewGuid().ToString();
        var connection = CreateConnection("user-1");

        var loaded = false;
        connection.On<object, object>("DocumentLoaded", (_, __) => loaded = true);

        await connection.StartAsync();

        //Act
        await connection.InvokeAsync("JoinDocument", documentId);
        await WaitUntil(() => loaded);

        //Assert
        Assert.True(loaded);
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task UpdateContent_BroadcastsToOthers()
    {
        //Arrange
        var documentId = Guid.NewGuid().ToString();

        var c1 = CreateConnection("user-1");
        var c2 = CreateConnection("user-2");

        var received = false;
        c2.On<string, string>("ContentUpdated", (_, __) => received = true);

        await c1.StartAsync();
        await c2.StartAsync();

        await c1.InvokeAsync("JoinDocument", documentId);
        await c2.InvokeAsync("JoinDocument", documentId);

        //Act
        await c1.InvokeAsync("UpdateContent", documentId, "hello");
        await WaitUntil(() => received);

        //Assert
        Assert.True(received);

        await c1.DisposeAsync();
        await c2.DisposeAsync();
    }
}
