using SyncWaves.Documents.Domain.Entities;

namespace SyncWaves.Documents.Domain.Abstractions;

public interface IDocumentPdfGenerator
{
    Task<byte[]> GenerateAsync(string title, string content, CancellationToken cancellationToken);
}
