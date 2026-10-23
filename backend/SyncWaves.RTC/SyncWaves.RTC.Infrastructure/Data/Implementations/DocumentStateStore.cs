using SyncWaves.RTC.Domain.Abstractions;
using System.Collections.Concurrent;


namespace SyncWaves.RTC.Infrastructure.Data.Implementations;

public class DocumentStateStore : IDocumentStateStore
{
    private readonly ConcurrentDictionary<string, string> _documents = new();

    public Task<string?> GetContentAsync(string documentId, CancellationToken ct = default)
    {
        _documents.TryGetValue(documentId, out var content);
        return Task.FromResult(content);
    }

    public Task<string> UpdateContentAsync(string documentId, string newContent, CancellationToken ct = default)
    {
        _documents[documentId] = newContent;
        return Task.FromResult(newContent);
    }
}
