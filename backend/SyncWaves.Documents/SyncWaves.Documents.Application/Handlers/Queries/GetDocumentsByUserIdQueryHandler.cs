using MediatR;
using SyncWaves.Documents.Application.DTOs;
using SyncWaves.Documents.Application.Extensions;
using SyncWaves.Documents.Application.Queries;
using SyncWaves.Documents.Application.Resources;
using SyncWaves.Documents.Domain.Abstractions;
using SyncWaves.Documents.Domain.Exceptions;

namespace SyncWaves.Documents.Application.Handlers.Queries;

public class GetDocumentsByUserIdQueryHandler : IRequestHandler<GetDocumentsByUserIdQuery, List<DocumentDTO>>
{
    private readonly IDocumentsRepository _documentsRepository;

    public GetDocumentsByUserIdQueryHandler(IDocumentsRepository documentsRepository)
    {
        _documentsRepository = documentsRepository;
    }

    public async Task<List<DocumentDTO>> Handle(GetDocumentsByUserIdQuery query, CancellationToken cancellationToken)
    {
        var documents = await _documentsRepository.GetByUserId(query.UserId, cancellationToken);

        if (documents == null || documents.Count == 0)
        {
            throw new DocumentNotFoundException("No_Documents");
        }

        return documents.Select(x => x.AsDTO()).ToList();
    }
}
