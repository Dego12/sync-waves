using SyncWaves.RTC.Domain.Entities;

namespace SyncWaves.RTC.Domain.Abstractions;

public interface IDocumentStateStore
{
    Task<string?> GetContentAsync(string documentId, CancellationToken ct = default);
    Task<string> UpdateContentAsync(string documentId, string newContent, CancellationToken ct = default);
}
