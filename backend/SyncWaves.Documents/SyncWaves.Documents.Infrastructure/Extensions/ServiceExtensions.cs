using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SyncWaves.Documents.Application.Handlers.Commands;
using SyncWaves.Documents.Application.Resources;
using SyncWaves.Documents.Domain.Abstractions;
using SyncWaves.Documents.Infrastructure.Data;
using SyncWaves.Documents.Infrastructure.Data.Repositories;
using SyncWaves.Documents.Infrastructure.Helpers;
using System.Globalization;
using System.Text;

namespace SyncWaves.Documents.Infrastructure.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var jwtSection = configuration.GetSection("Jwt");
        var key = jwtSection["Key"]!;
        var issuer = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];

        var mediatrSection = configuration.GetSection("MediatR");

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

        services.AddDbContext<DocumentsDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IDocumentsRepository, DocumentsRepository>();
        services.AddScoped<IDocumentPermissionsRepository, DocumentPermissionsRepository>();
        services.AddScoped<IDocumentPdfGenerator, DocumentPdfGenerator>();
        services.AddMediatR(cfg =>
        {
            cfg.LicenseKey = mediatrSection["LicenseKey"];
            cfg.RegisterServicesFromAssembly(typeof(CreateDocumentCommandHandler).Assembly);
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

        var appSection = configuration.GetSection("App");
        var frontendBaseUrl = appSection["FrontendBaseUrl"];
        var rtcBaseUrl = appSection["RtcBaseUrl"];
        services.AddCors(options =>
            options.AddPolicy("AllowFrontendAndRtc", policy =>
                policy
                  .WithOrigins(frontendBaseUrl!, rtcBaseUrl!)
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
