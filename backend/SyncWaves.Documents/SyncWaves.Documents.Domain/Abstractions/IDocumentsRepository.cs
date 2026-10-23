using SyncWaves.Documents.Domain.Entities;

namespace SyncWaves.Documents.Domain.Abstractions;

public interface IDocumentsRepository
{
    Task<DocumentEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<DocumentEntity>?> GetByUserId(Guid userId, CancellationToken ct = default);
    Task AddAsync(DocumentEntity document, CancellationToken ct = default);
    Task UpdateAsync(DocumentEntity document, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
