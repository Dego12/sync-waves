using Microsoft.EntityFrameworkCore;
using SyncWaves.Auth.Domain.Entities;
using SyncWaves.Auth.Infrastructure.Data;
using SyncWaves.Auth.Infrastructure.Data.Extensions;

public class RefreshTokensRepository : IRefreshTokensRepository
{
    private readonly AuthDbContext _dbContext;

    public RefreshTokensRepository(AuthDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(RefreshTokenEntity token, CancellationToken cancellationToken)
    {
        var model = token.AsModel();
        await _dbContext.RefreshTokens.AddAsync(model, cancellationToken);
    }

    public async Task<RefreshTokenEntity?> GetByTokenHashAsync(string token, CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(token);
        var existing = await _dbContext.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == token, cancellationToken);

        return existing?.AsEntity();
    }

    public Task SaveChangesAsync(CancellationToken ct) => _dbContext.SaveChangesAsync(ct);

    public string HashToken(string token)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
