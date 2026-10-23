using MediatR;

namespace SyncWaves.Documents.Application.Commands;

public class UpdateDocumentCommand : IRequest
{
    public Guid DocumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Version { get; set; }
}
