namespace SyncWaves.Auth.Domain.Exceptions;

public class EmailNotConfirmedException : Exception
{
    public EmailNotConfirmedException(string message) : base(message) { }
}
