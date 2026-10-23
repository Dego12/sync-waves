namespace SyncWaves.RTC.Domain.Entities;

public class DocumentStateEntity
{
    public Guid DocumentId { get; set; }
    public int CurrentVersion { get; set; }
    public string Content { get; set; }
    public DateTime LastUpdated { get; set; }

    public List<DocumentVersionEntity> Versions { get; private set; } = new();

    public DocumentStateEntity() { }

    public DocumentStateEntity(Guid documentId, string initialContent = "")
    {
        DocumentId = documentId;
        Content = initialContent;
        CurrentVersion = 0;
        LastUpdated = DateTime.UtcNow;
    }

    public DocumentVersionEntity ApplyNewContent(string newContent)
    {
        var newVersion = CurrentVersion + 1;

        var version = new DocumentVersionEntity(
            DocumentId,
            newVersion,
            newContent,
            DateTime.UtcNow);

        CurrentVersion = newVersion;
        Content = newContent;
        LastUpdated = version.CreatedAt;

        Versions.Add(version);

        return version;
    }
}
