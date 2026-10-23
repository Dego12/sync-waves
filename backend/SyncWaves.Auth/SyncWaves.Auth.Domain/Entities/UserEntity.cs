namespace SyncWaves.Auth.Domain.Entities;

public class UserEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public string? EmailConfirmationToken { get; set; }
    public DateTime? EmailConfirmationTokenExpiresAt { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiresAt { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? ExternalId { get; set; }
    public string? ExternalProvider { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<RefreshTokenEntity> RefreshTokens { get; set; } = [];

    public UserEntity() { }

    public UserEntity(string name, string email, string passwordHash)
    {
        Id = Guid.NewGuid();
        Name = name;
        Email = email.ToLower().Trim();
        PasswordHash = passwordHash;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetEmailConfirmation(string token, DateTime expiresAt)
    {
        EmailConfirmationToken = token;
        EmailConfirmationTokenExpiresAt = expiresAt;
        EmailConfirmed = false;
    }

    public void ConfirmEmail()
    {
        EmailConfirmed = true;
        EmailConfirmationToken = null;
        EmailConfirmationTokenExpiresAt = null;
    }

    public void SetPasswordResetToken(string token, DateTime expiresAt)
    {
        PasswordResetToken = token;
        PasswordResetTokenExpiresAt = expiresAt;
    }

    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;
    
    public void SetProfilePicture(string pictureUrl) => ProfilePictureUrl = pictureUrl;
}
