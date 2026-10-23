namespace SyncWaves.Auth.Domain.Abstractions;

public interface IProfileImageStorage 
{
    Task<string> SaveProfileImageAsync(Guid userId, Stream fileStream, string filename, string contentType, CancellationToken cancellationToken);
}

