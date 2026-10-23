namespace SyncWaves.RTC.Infrastructure.Data.Models;

public class DocumentStateModel
{
    public Guid DocumentId { get; set; }
    public int CurrentVersion { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }

    public List<DocumentVersionModel> Versions { get; private set; } = new();
}
