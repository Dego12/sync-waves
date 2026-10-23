using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SyncWaves.RTC.Api.Helpers;
using SyncWaves.RTC.Api.Hubs;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;

var jwtSection = builder.Configuration.GetSection("Jwt");
var key = jwtSection["Key"]!;
var issuer = jwtSection["Issuer"];
var audience = jwtSection["Audience"];

services.AddSignalR(o =>
{
    o.EnableDetailedErrors = true;
    o.MaximumReceiveMessageSize = 1024 * 1024;
});

services.AddSingleton<IDocumentStateStore, DocumentStateStore>();
services.AddSingleton<IDocumentSessionStore, DocumentSessionStore>();

services.AddControllers();
services.AddOpenApi();
services.AddEndpointsApiExplorer();

var frontendBaseUrl = builder.Configuration.GetSection("App")["FrontendBaseUrl"];
services.AddCors(options =>
    options.AddPolicy("AllowFrontend", policy =>
        policy
            .WithOrigins(frontendBaseUrl!)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
        )
);

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

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs/document"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

services.AddHttpClient("DocumentsApi", (sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["App:DocumentsBaseUrl"]!;
    client.BaseAddress = new Uri(baseUrl);
});


services.AddAuthorization();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRouting();
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<DocumentHub>("/hubs/document");

app.Run();
