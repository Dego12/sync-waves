namespace SyncWaves.Auth.Application.DTOs;

public class UserDTO
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
}
