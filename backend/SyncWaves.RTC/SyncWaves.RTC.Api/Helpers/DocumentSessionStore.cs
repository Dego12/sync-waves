using SyncWaves.RTC.Api.DTO;
using System.Collections.Concurrent;

namespace SyncWaves.RTC.Api.Helpers;

public class DocumentSessionStore : IDocumentSessionStore
{
    private readonly ConcurrentDictionary<string, DocumentSession> _sessions = new();

    public Task<DocumentSession> GetOrCreateAsync(string documentId)
    {
        var s = _sessions.GetOrAdd(documentId, id => new DocumentSession { DocumentId = id });
        return Task.FromResult(s);
    }

    public async Task AddOrUpdateUserAsync(string documentId, DocumentSessionUser user)
    {
        var s = await GetOrCreateAsync(documentId);
        lock (s.ConnectedUsers)
        {
            s.ConnectedUsers[user.ConnectionId] = user;
        }
    }

    public async Task RemoveUserAsync(string documentId, string connectionId)
    {
        var s = await GetOrCreateAsync(documentId);
        lock (s.ConnectedUsers)
        {
            s.ConnectedUsers.Remove(connectionId);
        }
    }

    public async Task<IReadOnlyCollection<DocumentSessionUser>> GetUsersAsync(string documentId)
    {
        var s = await GetOrCreateAsync(documentId);
        lock (s.ConnectedUsers)
        {
            return s.ConnectedUsers.Values.ToList();
        }
    }

    public async Task SetUserCanEditAsync(string documentId, string userId, bool canEdit)
    {
        var s = await GetOrCreateAsync(documentId);
        lock (s.ConnectedUsers)
        {
            foreach (var u in s.ConnectedUsers.Values.Where(x => x.UserId == userId))
            {
                u.CanEdit = canEdit;
            }
        }
    }

    public async Task<(string? DocumentId, DocumentSessionUser? User)> FindByConnectionIdAsync(string connectionId)
    {
        foreach (var session in _sessions.Values)
        {
            lock (session.ConnectedUsers)
            {
                if (session.ConnectedUsers.TryGetValue(connectionId, out var user))
                {
                    return (session.DocumentId, user);
                }
            }
        }

        return (null, null);
    }
}
