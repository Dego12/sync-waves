using MediatR;
using SyncWaves.Documents.Application.DTOs;
using SyncWaves.Documents.Application.Extensions;
using SyncWaves.Documents.Application.Queries;
using SyncWaves.Documents.Application.Resources;
using SyncWaves.Documents.Domain.Abstractions;
using SyncWaves.Documents.Domain.Exceptions;

namespace SyncWaves.Documents.Application.Handlers.Queries;

public class GetDocumentQueryHandler : IRequestHandler<GetDocumentQuery, DocumentDTO?>
{
    private readonly IDocumentsRepository _documentsRepository;

    public GetDocumentQueryHandler(IDocumentsRepository documentsRepository)
    {
        _documentsRepository = documentsRepository;
    }

    public async Task<DocumentDTO?> Handle(GetDocumentQuery query, CancellationToken cancellationToken)
    {
        var document = await _documentsRepository.GetByIdAsync(query.Id, cancellationToken);

        if (document == null)
        {
            throw new DocumentNotFoundException("Document_Does_Not_Exist");
        }
        return document.AsDTO();
    }
}
