namespace SyncWaves.Auth.Application.Abstractions;

public interface IPasswordService
{
    bool EnsureValid(string password);
    string Hash(string password);
    bool Verify(string password, string hash);
}
