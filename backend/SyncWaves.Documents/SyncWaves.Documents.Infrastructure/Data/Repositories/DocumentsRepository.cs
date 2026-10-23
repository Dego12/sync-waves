using MediatR;
using Microsoft.EntityFrameworkCore;
using SyncWaves.Documents.Domain.Abstractions;
using SyncWaves.Documents.Domain.Entities;
using SyncWaves.Documents.Domain.Exceptions;
using SyncWaves.Documents.Infrastructure.Data.Extensions;

namespace SyncWaves.Documents.Infrastructure.Data.Repositories;

public class DocumentsRepository : IDocumentsRepository
{
    private readonly DocumentsDbContext _dbContext;

    public DocumentsRepository(DocumentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DocumentEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await _dbContext.Documents.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return document?.AsEntity();
    }

    public async Task<List<DocumentEntity>?> GetByUserId(Guid userId, CancellationToken cancellationToken)
    {
        var documents = await _dbContext.Documents.Where(x => x.UserId == userId).Select(x => x.AsEntity()).ToListAsync(cancellationToken);

        return documents;
    }

    public async Task AddAsync(DocumentEntity document, CancellationToken cancellationToken)
    {
        await _dbContext.Documents.AddAsync(document.AsModel(), cancellationToken);
    }

    public async Task UpdateAsync(DocumentEntity document, CancellationToken cancellationToken)
    {
        try
        {
            var tracked = await _dbContext.Documents.SingleAsync(x => x.Id == document.Id, cancellationToken);

            tracked.Title = document.Title;
            tracked.Content = document.Content;
            tracked.UpdatedAt = document.UpdatedAt;

            var entry = _dbContext.Entry(tracked);
            entry.Property(d => d.Version).OriginalValue = document.Version;

            tracked.Version = document.Version + 1;

            await SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            var current = await _dbContext.Documents
                .AsNoTracking()
                .SingleAsync(d => d.Id == document.Id, cancellationToken);

            throw new DocumentConflictException(current.AsEntity());
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
