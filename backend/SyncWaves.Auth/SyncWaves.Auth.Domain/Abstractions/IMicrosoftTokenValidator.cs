using System.Security.Claims;

namespace SyncWaves.Auth.Domain.Abstractions;

public interface IMicrosoftTokenValidator
{
    Task<ClaimsPrincipal> ValidateIdTokenAsync(string idToken, CancellationToken cancellationToken);
}
