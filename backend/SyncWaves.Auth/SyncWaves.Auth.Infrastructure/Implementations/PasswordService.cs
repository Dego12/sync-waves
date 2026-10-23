using SyncWaves.Auth.Application.Abstractions;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SyncWaves.Auth.Infrastructure.Implementations;

public class PasswordService : IPasswordService
{
    private const int MinLength = 8;

    private readonly Regex PasswordRegex = new(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).+$", RegexOptions.Compiled);

    public bool EnsureValid(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinLength)
        {
            throw new ArgumentException("Password_Min_Length");
        }

        if (!PasswordRegex.IsMatch(password))
        {
            throw new ArgumentException("Password_Regex_Mismatch");
        }

        return true;
    }

    public string Hash(string password)
    {
        using var sha = SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(password)));
    }

    public bool Verify(string password, string hash)
        => Hash(password) == hash;
}