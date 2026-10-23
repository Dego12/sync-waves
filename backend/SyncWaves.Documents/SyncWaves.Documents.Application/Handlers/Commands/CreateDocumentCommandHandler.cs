using MediatR;
using SyncWaves.Documents.Application.Commands;
using SyncWaves.Documents.Application.DTOs;
using SyncWaves.Documents.Application.Extensions;
using SyncWaves.Documents.Domain.Abstractions;
using SyncWaves.Documents.Domain.Entities;

namespace SyncWaves.Documents.Application.Handlers.Commands;

public class CreateDocumentCommandHandler : IRequestHandler<CreateDocumentCommand, DocumentDTO>
{
    private readonly IDocumentsRepository _documentsRepository;

    public CreateDocumentCommandHandler(IDocumentsRepository documentsRepository)
    {
        _documentsRepository = documentsRepository;
    }

    public async Task<DocumentDTO> Handle(CreateDocumentCommand command, CancellationToken cancellationToken)
    {
        var document = new DocumentEntity(command.Title, command.Content, command.UserId);

        await _documentsRepository.AddAsync(document, cancellationToken);
        await _documentsRepository.SaveChangesAsync(cancellationToken);

        return document.AsDTO();
    }
}
