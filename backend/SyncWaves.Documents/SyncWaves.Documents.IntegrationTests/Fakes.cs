using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SyncWaves.Documents.Application.Resources;
using SyncWaves.Documents.Domain.Abstractions;
using SyncWaves.Documents.Domain.Entities;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

public class InMemoryDocumentsRepository : IDocumentsRepository
{
    private readonly ConcurrentDictionary<Guid, DocumentEntity> _byId = new();

    public Task<DocumentEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _byId.TryGetValue(id, out var doc);
        return Task.FromResult(doc);
    }

    public Task<List<DocumentEntity>?> GetByUserId(Guid userId, CancellationToken ct = default)
    {
        var docs = _byId.Values.Where(x => x.UserId == userId).OrderBy(x => x.CreatedAt)?.ToList();
        return Task.FromResult(docs);
    }

    public Task AddAsync(DocumentEntity document, CancellationToken ct = default)
    {
        _byId[document.Id] = document;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(DocumentEntity document, CancellationToken ct = default)
    {
        _byId[document.Id] = document;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public void Clear()
    {
        _byId.Clear();
    }

    public void Seed(DocumentEntity entity)
    {
        _byId[entity.Id] = entity;
    }
}

public class InMemoryCollaboratorsRepository : IDocumentPermissionsRepository
{
    private readonly InMemoryDocumentsRepository _documents;
    private readonly ConcurrentDictionary<(Guid DocumentId, Guid UserId), bool> _canEdit = new();

    public InMemoryCollaboratorsRepository(InMemoryDocumentsRepository documents)
    {
        _documents = documents;
    }

    public async Task<DocumentCollaboratorEntity> GetPermissionsForUserAsync(Guid documentId, Guid userId, CancellationToken cancellationToken = default)
    {
        var doc = await _documents.GetByIdAsync(documentId, cancellationToken);
        if (doc == null)
            throw new ArgumentException("Document_Not_Found");

        var isOwner = doc.UserId == userId;

        var has = _canEdit.TryGetValue((documentId, userId), out var canEdit);
        var effectiveCanEdit = isOwner || (has && canEdit);

        return new DocumentCollaboratorEntity
        {
            DocumentId = documentId,
            UserId = userId,
            IsOwner = isOwner,
            CanEdit = effectiveCanEdit
        };
    }

    public async Task SetEditPermissionAsync(Guid documentId, Guid ownerId, Guid targetUserId, bool canEdit, CancellationToken cancellationToken = default)
    {
        var doc = await _documents.GetByIdAsync(documentId, cancellationToken);
        if (doc == null)
            throw new ArgumentException("Document_Not_Found");

        if (doc.UserId != ownerId)
            throw new UnauthorizedAccessException("Only_Owner_Changes");

        _canEdit[(documentId, targetUserId)] = canEdit;
    }
}


public sealed class FakePdfGenerator : IDocumentPdfGenerator
{
    public List<(string Title, string Content)> Requests { get; } = [];

    public Task<byte[]> GenerateAsync(string title, string content, CancellationToken cancellationToken)
    {
        Requests.Add((title, content));
        var bytes = Encoding.UTF8.GetBytes($"PDF::{title}::{content}");
        return Task.FromResult(bytes);
    }

    public void Clear()
    {
        Requests.Clear();
    }
}

public sealed class DictionaryResourceLocalizer : IResourceLocalizer
{
    public string Localize(string key)
    {
        return key;
    }
}

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string UserIdHeader = "X-Test-UserId";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Context.Request.Headers.TryGetValue(UserIdHeader, out var uid)
            ? uid.ToString()
            : Guid.Parse("11111111-1111-1111-1111-111111111111").ToString();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, "test-user"),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
