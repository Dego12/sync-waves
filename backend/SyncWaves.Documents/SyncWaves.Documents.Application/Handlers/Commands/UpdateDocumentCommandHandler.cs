using MediatR;
using SyncWaves.Documents.Application.Commands;
using SyncWaves.Documents.Application.Resources;
using SyncWaves.Documents.Domain.Abstractions;
using SyncWaves.Documents.Domain.Exceptions;

namespace SyncWaves.Documents.Application.Handlers.Commands;

public class UpdateDocumentCommandHandler : IRequestHandler<UpdateDocumentCommand>
{
    private readonly IDocumentsRepository _documentsRepository;

    public UpdateDocumentCommandHandler(IDocumentsRepository documentsRepository)
    {
        _documentsRepository = documentsRepository;
    }

    public async Task Handle(UpdateDocumentCommand command, CancellationToken cancellationToken)
    {
        var doc = await _documentsRepository.GetByIdAsync(command.DocumentId);

        if (doc == null)
        {
            throw new DocumentNotFoundException("Document_Does_Not_Exist");
        }

        doc.UpdateContent(command.Content, command.Title, command.Version);

        await _documentsRepository.UpdateAsync(doc, cancellationToken);
    }
}
