using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using SyncWaves.Auth.Domain.Abstractions;
using SyncWaves.Auth.Infrastructure.Settings;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

public class MicrosoftTokenValidator : IMicrosoftTokenValidator 
{ 
    private readonly ConfigurationManager<OpenIdConnectConfiguration> _configManager;
    private readonly IConfiguration _configuration;

    public MicrosoftTokenValidator(IConfiguration configuration, ILogger<MicrosoftTokenValidator> logger) 
    {
        _configuration = configuration;
        var msSection = _configuration.GetSection("MicrosoftIdentity");
        var instance = msSection["Instance"]!;

        var metadataAddress = $"{instance}{msSection["TenantId"]}/v2.0/.well-known/openid-configuration";
        _configManager = new ConfigurationManager<OpenIdConnectConfiguration>(metadataAddress, new OpenIdConnectConfigurationRetriever()); 
    } 
    
    public async Task<ClaimsPrincipal> ValidateIdTokenAsync(string idToken, CancellationToken ct)
    {
        var msSection = _configuration.GetSection("MicrosoftIdentity");
        var config = await _configManager.GetConfigurationAsync(ct); 
        var tokenHandler = new JwtSecurityTokenHandler(); 
        var validationParameters = new TokenValidationParameters 
        { 
            ValidIssuer = $"{msSection["Instance"]}{msSection["TenantId"]}/v2.0", 
            ValidateIssuer = true, 
            ValidateAudience = true, 
            ValidAudience = msSection["ClientId"], 
            IssuerSigningKeys = config.SigningKeys, 
            ValidateLifetime = true, 
            ClockSkew = TimeSpan.FromMinutes(5) 
        }; 
        
        return tokenHandler.ValidateToken(idToken, validationParameters, out _); 
    }
}