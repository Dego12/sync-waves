using SyncWaves.RTC.Api.DTO;

namespace SyncWaves.RTC.Api.Helpers;

public interface IDocumentStateStore
{
    Task<string?> GetContentAsync(string documentId, CancellationToken cancellationToken = default);
    Task<string> UpdateContentAsync(string documentId, string newContent, CancellationToken cancellationToken = default);
    Task<string?> GetTitleAsync(string documentId, CancellationToken cancellationToken = default);
    Task<string> UpdateTitleAsync(string documentId, string newTitle, CancellationToken cancellationToken = default);
}
