namespace SyncWaves.Auth.Domain.Entities;

public class RefreshTokenEntity 
{
    public Guid Id { get; set; }
    public string TokenHash { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? CreatedByIp { get; set; }
    public Guid UserId { get; set; }
    public UserEntity? User { get; set; }
    public bool IsActive => RevokedAt == null && ExpiresAt > DateTime.UtcNow;
}
