using SyncWaves.Documents.Domain.Entities;

namespace SyncWaves.Documents.Infrastructure.Data.Models;

public class DocumentCollaboratorModel
{
    public Guid DocumentId { get; set; }
    public Guid UserId { get; set; }
    public bool CanEdit { get; set; }
    public DocumentModel Document { get; set; } = default!;
}
