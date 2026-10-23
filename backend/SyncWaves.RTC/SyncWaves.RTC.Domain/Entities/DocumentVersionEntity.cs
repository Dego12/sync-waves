namespace SyncWaves.RTC.Domain.Entities;

public class DocumentVersionEntity
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int VersionNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public DocumentVersionEntity() { }

    public DocumentVersionEntity(Guid documentId, int versionNumber, string content, DateTime createdAt)
    {
        DocumentId = documentId;
        VersionNumber = versionNumber;
        Content = content;
        CreatedAt = createdAt;
    }
}
