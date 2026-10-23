namespace SyncWaves.Documents.Domain.Exceptions;

public class DocumentNotFoundException : Exception
{
    public DocumentNotFoundException(string message) : base(message) { }
}
