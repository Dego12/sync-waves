namespace SyncWaves.RTC.Infrastructure.Data.Models;

public class DocumentVersionModel
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int VersionNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
