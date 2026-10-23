namespace SyncWaves.RTC.Api.DTO;

public class DocumentSessionUser
{
    public string ConnectionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public bool IsOwner { get; set; }
    public bool CanEdit { get; set; }
}

public class DocumentSession
{
    public string DocumentId { get; set; } = string.Empty;
    public Dictionary<string, DocumentSessionUser> ConnectedUsers { get; } = new();
}

