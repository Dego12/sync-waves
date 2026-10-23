using Microsoft.EntityFrameworkCore;
using SyncWaves.Auth.Domain.Entities;
using SyncWaves.Auth.Infrastructure.Data.Extensions;

namespace SyncWaves.Auth.Infrastructure.Data.Repositories;

public class UsersRepository : IUsersRepository
{
    private readonly AuthDbContext _authDbContext;

    public UsersRepository(AuthDbContext authDbContext)
    {
        _authDbContext = authDbContext;
    }

    public async Task<UserEntity?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _authDbContext.Users.FirstOrDefaultAsync(x => string.Equals(x.Email, email), cancellationToken);

        return user?.AsEntity();
    }

    public async Task<UserEntity?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _authDbContext.Users.FirstOrDefaultAsync(x => x.Id ==  userId, cancellationToken);

        return user?.AsEntity();
    }

    public async Task AddAsync(UserEntity user, CancellationToken cancellationToken = default)
    {
        await _authDbContext.Users.AddAsync(user.AsModel(), cancellationToken);
    }

    public async Task UpdateAsync(UserEntity user, CancellationToken cancellationToken = default)
    {
        var tracked = await _authDbContext.Users.SingleAsync(x => x.Id == user.Id, cancellationToken);
        
        if (tracked != null)
        {
            tracked.Name = user.Name;
            tracked.Email = user.Email;
            tracked.PasswordHash = user.PasswordHash;
            tracked.EmailConfirmed = user.EmailConfirmed;
            tracked.PasswordResetToken = user.PasswordResetToken;
            tracked.PasswordResetTokenExpiresAt = user.PasswordResetTokenExpiresAt;
            tracked.ProfilePictureUrl = user.ProfilePictureUrl;
            tracked.UpdatedAt = DateTime.UtcNow;

            _authDbContext.Users.Update(tracked);
        }
    }

    public async Task<UserEntity?> GetByExternalIdAsync(string provider, string externalId, CancellationToken cancellationToken)
    {
        var user = await _authDbContext.Users.SingleOrDefaultAsync(x => x.ExternalProvider == provider && x.ExternalId == externalId, cancellationToken);

        return user?.AsEntity();
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _authDbContext.SaveChangesAsync(cancellationToken);
    }
}
