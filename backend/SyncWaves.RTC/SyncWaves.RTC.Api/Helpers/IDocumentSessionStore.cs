using SyncWaves.RTC.Api.DTO;

namespace SyncWaves.RTC.Api.Helpers;

public interface IDocumentSessionStore
{
    Task<DocumentSession> GetOrCreateAsync(string documentId);
    Task AddOrUpdateUserAsync(string documentId, DocumentSessionUser user);
    Task RemoveUserAsync(string documentId, string connectionId);
    Task<IReadOnlyCollection<DocumentSessionUser>> GetUsersAsync(string documentId);
    Task SetUserCanEditAsync(string documentId, string userId, bool canEdit);
    Task<(string? DocumentId, DocumentSessionUser? User)> FindByConnectionIdAsync(string connectionId);
}
