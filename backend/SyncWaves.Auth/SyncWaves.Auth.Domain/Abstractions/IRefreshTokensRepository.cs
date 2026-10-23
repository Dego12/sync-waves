using SyncWaves.Auth.Domain.Abstractions;
using SyncWaves.Auth.Domain.Entities;

public interface IRefreshTokensRepository
{
    Task AddAsync(RefreshTokenEntity token, CancellationToken cancellationToken);
    Task<RefreshTokenEntity?> GetByTokenHashAsync(string token, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    string HashToken(string token);
}
