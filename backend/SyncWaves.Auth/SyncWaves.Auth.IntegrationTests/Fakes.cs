using SyncWaves.Auth.Application.Resources;
using SyncWaves.Auth.Domain.Abstractions;
using SyncWaves.Auth.Domain.Entities;
using System.Collections.Concurrent;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

public class InMemoryUsersRepository : IUsersRepository
{
    private readonly ConcurrentDictionary<Guid, UserEntity> _byId = new();
    private readonly ConcurrentDictionary<string, Guid> _emailToId = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, UserEntity> _byExternalId = new();
    private readonly ConcurrentQueue<UserEntity> _added = new();
    public IReadOnlyCollection<UserEntity> Added => _added.ToArray();

    public Task<UserEntity?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (_emailToId.TryGetValue(email.Trim().ToLowerInvariant(), out var id) && _byId.TryGetValue(id, out var user))
        {
            return Task.FromResult<UserEntity?>(user);
        }
        return Task.FromResult<UserEntity?>(null);
    }

    public Task<UserEntity?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult(_byId.TryGetValue(userId, out var user) ? user : null);

    public Task AddAsync(UserEntity user, CancellationToken cancellationToken = default)
    {
        _byId[user.Id] = user;
        if (user.ExternalId != null)
        {
            _byExternalId[user.ExternalId] = user;
        }
        _emailToId[user.Email.Trim().ToLowerInvariant()] = user.Id;
        _added.Enqueue(user);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(UserEntity user, CancellationToken cancellationToken = default)
    {
        _byId[user.Id] = user;
        _emailToId[user.Email.Trim().ToLowerInvariant()] = user.Id;
        return Task.CompletedTask;
    }

    public Task<UserEntity?> GetByExternalIdAsync(string provider, string externalId, CancellationToken cancellationToken = default)
        => Task.FromResult(_byExternalId.TryGetValue(externalId, out var user) ? user : null);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Clear()
    {
        _byId.Clear();
        _emailToId.Clear();
    }
}

public sealed class InMemoryRefreshTokensRepository : IRefreshTokensRepository
{
    private readonly ConcurrentDictionary<string, RefreshTokenEntity> _byHash = new();

    private readonly ConcurrentQueue<RefreshTokenEntity> _added = new();
    public IReadOnlyCollection<RefreshTokenEntity> Added => _added.ToArray();

    public Task AddAsync(RefreshTokenEntity token, CancellationToken cancellationToken)
    {
        var hash = GetHashFromEntity(token);

        _byHash[hash] = token;
        _added.Enqueue(token);
        return Task.CompletedTask;
    }

    public Task<RefreshTokenEntity?> GetByTokenHashAsync(string token, CancellationToken cancellationToken)
    {
        return Task.FromResult(_byHash.TryGetValue(token, out var stored) ? stored : null);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public string HashToken(string token)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public void Clear()
    {
        _byHash.Clear();
        while (_added.TryDequeue(out _)) { }
    }

    private string GetHashFromEntity(RefreshTokenEntity token)
    {
        var tokenHashProp = token.GetType().GetProperty("TokenHash");
        if (tokenHashProp?.GetValue(token) is string tokenHash && !string.IsNullOrWhiteSpace(tokenHash))
            return tokenHash;

        var tokenProp = token.GetType().GetProperty("Token");
        if (tokenProp?.GetValue(token) is string raw && !string.IsNullOrWhiteSpace(raw))
            return HashToken(raw);

        throw new InvalidOperationException(
            "RefreshTokenEntity must expose either TokenHash or Token so the fake can store it.");
    }
}

public sealed class FakeMicrosoftTokenValidator : IMicrosoftTokenValidator
{
    private readonly ConcurrentDictionary<string, ClaimsPrincipal> _principals = new(StringComparer.Ordinal);

    public void SetPrincipal(string idToken, ClaimsPrincipal principal) => _principals[idToken] = principal;

    public void Clear() => _principals.Clear();

    public Task<ClaimsPrincipal> ValidateIdTokenAsync(string idToken, CancellationToken ct)
    {
        if (!_principals.TryGetValue(idToken, out var principal))
            throw new InvalidCredentialException("Invalid Microsoft token.");

        return Task.FromResult(principal);
    }
}

public sealed record SentEmail(string To, string Subject, string HtmlBody);

public sealed class FakeEmailService : IEmailService
{
    private readonly List<SentEmail> _sent = new();
    public IReadOnlyList<SentEmail> Sent => _sent;

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        _sent.Add(new SentEmail(to, subject, htmlBody));
        return Task.CompletedTask;
    }

    public void Clear() => _sent.Clear();
}

public sealed class DictionaryResourceLocalizer : IResourceLocalizer
{
    private static readonly Dictionary<string, string> Resx = new()
    {
        ["Register_Email_Already_Registered"] = "User already registered.",
        ["Register_Email_Confirmation_Subject"] = "SyncWaves - confirm your email",
        ["Register_Email_Confirmation_Html"] = "<p>Confirm email: <a href=\"{0}\">link</a></p>",

        ["Login_Invalid_Credentials"] = "Invalid credentials.",
        ["Login_Email_Not_Confirmed"] = "Email not confirmed.",

        ["Invalid_Token"] = "Invalid token.",
        ["Confirm_Email_Already_Confirmed"] = "Email already confirmed.",
        ["Invalid_Expired_Token"] = "Invalid or expired token.",

        ["User_Does_Not_Exist"] = "User does not exist.",
        ["Reset_Password_Subject"] = "SyncWaves - reset password",
        ["Reset_Password_Html"] = "<p>Reset password: <a href=\"{0}\">link</a></p>",

        ["Password_Min_Length"] = "Password too short.",
        ["Password_Regex_Mismatch"] = "Password must contain upper/lower/digit/special."
    };

    public string Localize(string key)
        => Resx.TryGetValue(key, out var v) ? v : $"MISSING:{key}";
}
