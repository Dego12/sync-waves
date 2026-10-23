using MediatR;
using SyncWaves.Documents.Application.DTOs;
using SyncWaves.Documents.Domain.Entities;

namespace SyncWaves.Documents.Application.Queries;

public class GetDocumentQuery : IRequest<DocumentDTO?>
{
    public Guid Id { get; set; }
}
