using SyncWaves.Auth.Domain.Entities;
using SyncWaves.Auth.Infrastructure.Data.Models;

namespace SyncWaves.Auth.Infrastructure.Data.Extensions;

public static class UserExtensions
{
    extension(UserModel user)
    {
        public UserEntity AsEntity() =>
            new UserEntity
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                PasswordHash = user.PasswordHash,
                EmailConfirmationToken = user.EmailConfirmationToken,
                EmailConfirmationTokenExpiresAt = user.EmailConfirmationTokenExpiresAt,
                EmailConfirmed = user.EmailConfirmed,
                PasswordResetToken = user.PasswordResetToken,
                PasswordResetTokenExpiresAt = user.PasswordResetTokenExpiresAt,
                ProfilePictureUrl = user.ProfilePictureUrl,
                ExternalId = user.ExternalId,
                ExternalProvider = user.ExternalProvider,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
    }

    extension(UserEntity user)
    {
        public UserModel AsModel() =>
            new UserModel
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                PasswordHash = user.PasswordHash,
                EmailConfirmationToken = user.EmailConfirmationToken,
                EmailConfirmationTokenExpiresAt = user.EmailConfirmationTokenExpiresAt,
                EmailConfirmed = user.EmailConfirmed,
                PasswordResetToken = user.PasswordResetToken,
                PasswordResetTokenExpiresAt = user.PasswordResetTokenExpiresAt,
                ProfilePictureUrl = user.ProfilePictureUrl,
                ExternalId = user.ExternalId,
                ExternalProvider = user.ExternalProvider,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
    }

    extension(RefreshTokenModel token)
    {
        public RefreshTokenEntity AsEntity() =>
            new RefreshTokenEntity
            {
                Id = token.Id,
                TokenHash = token.TokenHash,
                CreatedAt = token.CreatedAt,
                ExpiresAt = token.ExpiresAt,
                RevokedAt = token.RevokedAt,
                ReplacedByTokenHash = token.ReplacedByTokenHash,
                CreatedByIp = token.CreatedByIp,
                UserId = token.UserId,
                User = token.User?.AsEntity()
            };
    }

    extension(RefreshTokenEntity token)
    {
        public RefreshTokenModel AsModel() =>
            new RefreshTokenModel
            {
                Id = token.Id,
                TokenHash = token.TokenHash,
                CreatedAt = token.CreatedAt,
                ExpiresAt = token.ExpiresAt,
                RevokedAt = token.RevokedAt,
                ReplacedByTokenHash = token.ReplacedByTokenHash,
                CreatedByIp = token.CreatedByIp,
                UserId = token.UserId,
                User = token.User?.AsModel()
            };
    }
}
