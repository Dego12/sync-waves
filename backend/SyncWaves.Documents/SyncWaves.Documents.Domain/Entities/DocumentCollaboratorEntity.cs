namespace SyncWaves.Documents.Domain.Entities;

public class DocumentCollaboratorEntity
{
    public Guid DocumentId { get; set; }
    public Guid UserId { get; set; }
    public bool CanEdit { get; set; }
    public bool IsOwner { get; set; }
    public DocumentEntity Document { get; set; } = default!;
}
