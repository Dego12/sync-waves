namespace SyncWaves.Auth.Infrastructure.Data.Models;

public class RefreshTokenModel 
{
    public Guid Id { get; set; }
    public string TokenHash { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? CreatedByIp { get; set; }
    public Guid UserId { get; set; }
    public UserModel? User { get; set; }
}
