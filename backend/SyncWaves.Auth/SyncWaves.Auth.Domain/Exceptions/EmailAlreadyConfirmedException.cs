namespace SyncWaves.Auth.Domain.Exceptions;

public class EmailAlreadyConfirmedException : Exception
{
    public EmailAlreadyConfirmedException(string message) : base(message) { }
}
