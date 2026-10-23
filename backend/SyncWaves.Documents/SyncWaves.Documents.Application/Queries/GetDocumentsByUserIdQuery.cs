using MediatR;
using SyncWaves.Documents.Application.DTOs;

namespace SyncWaves.Documents.Application.Queries;

public class GetDocumentsByUserIdQuery : IRequest<List<DocumentDTO>>
{
    public Guid UserId { get; set; }
}
