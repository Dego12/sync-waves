namespace SyncWaves.Auth.Domain.Exceptions;

public class TokenInvalidException : Exception
{
    public TokenInvalidException(string message) : base(message) { }
}
