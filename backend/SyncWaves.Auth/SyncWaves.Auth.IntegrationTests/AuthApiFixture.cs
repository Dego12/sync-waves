using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SyncWaves.Auth.Api.Middlewares;
using SyncWaves.Auth.Application.Abstractions;
using SyncWaves.Auth.Application.Handlers.Commands;
using SyncWaves.Auth.Application.Resources;
using SyncWaves.Auth.Domain.Abstractions;
using SyncWaves.Auth.Infrastructure.Implementations;
using System.IdentityModel.Tokens.Jwt;

public class AuthApiFixture : IAsyncLifetime
{
    private WebApplication? _app;

    public HttpClient Client { get; private set; } = default!;
    public InMemoryUsersRepository Users { get; private set; } = default!;
    public FakeEmailService Emails { get; private set; } = default!;
    public InMemoryRefreshTokensRepository RefreshTokens { get; private set; } = default!;
    public FakeMicrosoftTokenValidator MicrosoftTokens { get; private set; } = default!;
    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing"
        });

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["App:FrontendBaseUrl"] = "https://frontend.example",
            ["Jwt:Key"] = "THIS_IS_A_TEST_KEY_32CHARS_MIN!!",
            ["Jwt:Issuer"] = "syncwaves-tests",
            ["Jwt:Audience"] = "syncwaves-tests",
            ["MediatR:LicenseKey"] = ""
        });

        builder.WebHost.UseTestServer();

        var services = builder.Services;

        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
        services.AddControllers()
            .AddApplicationPart(typeof(AuthController).Assembly);

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(RegisterCommandHandler).Assembly);
        });

        services.AddScoped<IPasswordService, PasswordService>();
        services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        Users = new InMemoryUsersRepository();
        Emails = new FakeEmailService();
        RefreshTokens = new InMemoryRefreshTokensRepository();
        MicrosoftTokens = new FakeMicrosoftTokenValidator();

        services.AddSingleton<IUsersRepository>(Users);
        services.AddSingleton<IEmailService>(Emails);
        services.AddSingleton<IRefreshTokensRepository>(RefreshTokens);
        services.AddSingleton<IMicrosoftTokenValidator>(MicrosoftTokens);
        services.AddSingleton<IResourceLocalizer>(new DictionaryResourceLocalizer());
        services.AddScoped<IHttpContextAccessor,  HttpContextAccessor>();

        _app = builder.Build();

        _app.UseMiddleware<AuthExceptionHandlingMiddleware>();
        _app.MapControllers();

        await _app.StartAsync();
        Client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }
}
