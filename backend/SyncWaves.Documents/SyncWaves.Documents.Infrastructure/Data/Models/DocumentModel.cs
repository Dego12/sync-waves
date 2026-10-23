namespace SyncWaves.Documents.Infrastructure.Data.Models;

public class DocumentModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "Untitled";
    public string Content { get; set; } = string.Empty;
    public int Version { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<DocumentCollaboratorModel> Collaborators { get; set; } = [];
}
