namespace SyncWaves.Documents.Domain.Entities;

public class DocumentEntity
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "Untitled";
    public string Content { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<DocumentCollaboratorEntity> Collaborators { get; set; } = [];

    public Guid UserId { get; set; }

    public DocumentEntity() { }

    public DocumentEntity(string title, string content, Guid userId)
    {
        Id = Guid.NewGuid();
        Title = string.IsNullOrWhiteSpace(title) ? "Untitled" : title.Trim();
        Content = content;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        UserId = userId;
    }

    public void UpdateContent(string newContent, string title, int version)
    {
        Content = newContent ?? string.Empty;
        Title = title ?? "Untitled";
        Version = version;
        UpdatedAt = DateTime.UtcNow;
    }
}