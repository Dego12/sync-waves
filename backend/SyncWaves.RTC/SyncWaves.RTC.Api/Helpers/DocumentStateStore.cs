using SyncWaves.RTC.Api.DTO;
using System.Collections.Concurrent;

namespace SyncWaves.RTC.Api.Helpers;

public class DocumentStateStore : IDocumentStateStore
{
    private readonly ConcurrentDictionary<string, string> _contents = new();
    private readonly ConcurrentDictionary<string, string> _titles = new();

    public Task<string?> GetContentAsync(string documentId, CancellationToken cancellationToken = default)
    {
        _contents.TryGetValue(documentId, out var content);
        return Task.FromResult(content);
    }

    public Task<string> UpdateContentAsync(string documentId, string newContent, CancellationToken cancellationToken = default)
    {
        _contents[documentId] = newContent;
        return Task.FromResult(newContent);
    }

    public Task<string?> GetTitleAsync(string documentId, CancellationToken cancellationToken = default)
    {
        _titles.TryGetValue(documentId, out var title);
        return Task.FromResult(title);
    }

    public Task<string> UpdateTitleAsync(string documentId, string newTitle, CancellationToken cancellationToken = default)
    {
        _titles[documentId] = newTitle;
        return Task.FromResult(newTitle);
    }
}
