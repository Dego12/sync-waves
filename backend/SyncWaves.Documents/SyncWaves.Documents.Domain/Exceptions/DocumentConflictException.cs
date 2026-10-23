using SyncWaves.Documents.Domain.Entities;

namespace SyncWaves.Documents.Domain.Exceptions;

public class DocumentConflictException : Exception
{
    public DocumentEntity CurrentDocument { get; set; }

    public DocumentConflictException(DocumentEntity currentDocument) : base() 
    {
        CurrentDocument = currentDocument;
    }
}
