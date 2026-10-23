using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SyncWaves.Auth.Application.Abstractions;
using SyncWaves.Auth.Application.Handlers.Commands;
using SyncWaves.Auth.Application.Resources;
using SyncWaves.Auth.Domain.Abstractions;
using SyncWaves.Auth.Infrastructure.Data;
using SyncWaves.Auth.Infrastructure.Data.Repositories;
using SyncWaves.Auth.Infrastructure.Implementations;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace SyncWaves.Auth.Infrastructure.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("DefaultConnection");
        var mediatrSection = config.GetSection("MediatR");
        var jwtSection = config.GetSection("Jwt");
        var key = jwtSection["Key"]!;
        var issuer = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];

        services.Configure<MicrosoftIdentityOptions>(
            config.GetSection("MicrosoftIdentity")
        );

        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

        services.AddDbContext<AuthDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IUsersRepository, UsersRepository>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IProfileImageStorage, AzureBlobProfileImageStorage>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IRefreshTokensRepository, RefreshTokensRepository>();

        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.Configure<MicrosoftIdentityOptions>(config.GetSection("MicrosoftIdentity"));
        services.AddSingleton<IMicrosoftTokenValidator, MicrosoftTokenValidator>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization();

        services.AddMediatR(cfg =>
        {
            cfg.LicenseKey = mediatrSection["LicenseKey"];
            cfg.RegisterServicesFromAssembly(typeof(RegisterCommandHandler).Assembly);
        });

        services.AddLocalization(options => options.ResourcesPath = "Resources");
        services.AddScoped<IResourceLocalizer, ResourceLocalizer>();

        var supportedCultures = new[] { new CultureInfo("en"), new CultureInfo("ro") };
        services.Configure<RequestLocalizationOptions>(options =>
        {
            options.DefaultRequestCulture = new RequestCulture("en");
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;

            options.RequestCultureProviders = new List<IRequestCultureProvider>
            {
                new AcceptLanguageHeaderRequestCultureProvider()
            };
        });

        services.Configure<JwtOptions>(config.GetSection("Jwt"));
        services.AddOpenApi();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            const string schemeId = "bearer";

            options.AddSecurityDefinition(schemeId, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Authorization for SyncWaves"
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(schemeId, document)] = []
            });
        });
        var frontendBaseUrl = config.GetSection("App")["FrontendBaseUrl"];
        services.AddCors(options =>
            options.AddPolicy("AllowFrontend", policy =>
                policy.WithOrigins(frontendBaseUrl!)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                )
        );

        services.AddControllers()
            .AddViewLocalization()
            .AddDataAnnotationsLocalization();

        return services;
    }

}
