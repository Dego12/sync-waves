using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SyncWaves.RTC.Api.DTO;
using SyncWaves.RTC.Api.Helpers;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace SyncWaves.RTC.Api.Hubs;

[Authorize]
public class DocumentHub : Hub
{
    private readonly IDocumentStateStore _statesStore;
    private readonly IDocumentSessionStore _sessionsStore;
    private readonly IHttpClientFactory _httpClientFactory;

    public DocumentHub(IDocumentStateStore statesStore, IDocumentSessionStore sessionsStore, IHttpClientFactory httpClientFactory)
    {
        _statesStore = statesStore;
        _sessionsStore = sessionsStore;
        _httpClientFactory = httpClientFactory;
    }

    public async Task JoinDocument(string documentId)
    {
        if (!Guid.TryParse(documentId, out _))
            throw new HubException("Invalid Id.");

        await Groups.AddToGroupAsync(Context.ConnectionId, documentId);

        var (userId, displayName) = GetUserInfo();
        var perms = await GetPermissionsForUserAsync(documentId);

        var session = await _sessionsStore.GetOrCreateAsync(documentId);
        var color = GetColorForUser(userId);

        var user = new DocumentSessionUser
        {
            ConnectionId = Context.ConnectionId,
            UserId = userId,
            DisplayName = displayName,
            Color = color,
            IsOwner = perms.isOwner,
            CanEdit = perms.canEdit
        };

        await _sessionsStore.AddOrUpdateUserAsync(documentId, user);

        var title = await _statesStore.GetTitleAsync(documentId);
        var content = await _statesStore.GetContentAsync(documentId);

        await Clients.Caller.SendAsync("DocumentLoaded", title, content);

        await UpdatePresence(documentId);
    }

    public async Task UpdateContent(string documentId, string newContent)
    {
        var user = await GetCurrentSessionUser(documentId);
        
        if (user is null || !user.CanEdit)
            throw new HubException("You are not allowed to edit this document.");

        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

        var state = await _statesStore.UpdateContentAsync(documentId, newContent);

        await Clients.OthersInGroup(documentId).SendAsync("ContentUpdated", newContent, userId);
    }

    public async Task UpdateTitle(string documentId, string newTitle)
    {
        var user = await GetCurrentSessionUser(documentId);

        if (user is null || !user.CanEdit)
            throw new HubException("You are not allowed to edit this document.");

        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

        var state = await _statesStore.UpdateTitleAsync(documentId, newTitle);

        await Clients.OthersInGroup(documentId).SendAsync("TitleUpdated", newTitle, userId);
    }

    public async Task UpdateCursor(string documentId, int index, int length)
    {
        var user = await GetCurrentSessionUser(documentId);
        if (user is null) return;

        await Clients.OthersInGroup(documentId).SendAsync(
            "CursorUpdated",
            new
            {
                userId = user.UserId,
                index,
                length,
                color = user.Color,
                displayName = user.DisplayName
            });
    }

    private async Task<(bool isOwner, bool canEdit)> GetPermissionsForUserAsync(string documentId)
    {
        var http = _httpClientFactory.CreateClient("DocumentsApi");

        var accessToken = Context
            .GetHttpContext()?
            .Request
            .Query["access_token"]
            .ToString();

        if (!string.IsNullOrEmpty(accessToken))
        {
            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
        }

        var response = await http.GetAsync($"/documents/{documentId}/permissions/me");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        bool isOwner = doc.RootElement.GetProperty("isOwner").GetBoolean();
        bool canEdit = doc.RootElement.GetProperty("canEdit").GetBoolean();

        return (isOwner, canEdit);
    }

    public async Task SetEditPermission(string documentId, string targetUserId, bool canEdit)
    {
        var (callerUserId, _) = GetUserInfo();

        var http = _httpClientFactory.CreateClient("DocumentsApi");

        var accessToken = Context
            .GetHttpContext()?
            .Request
            .Query["access_token"]
            .ToString();

        if (!string.IsNullOrEmpty(accessToken))
        {
            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
        }

        var payload = JsonContent.Create(new { userId = targetUserId, canEdit });
        var response = await http.PostAsync($"/documents/{documentId}/permissions", payload);

        if (!response.IsSuccessStatusCode)
        {
            var msg = await response.Content.ReadAsStringAsync();
            throw new HubException($"Failed to update permissions: {msg}");
        }

        await _sessionsStore.SetUserCanEditAsync(documentId, targetUserId, canEdit);

        await UpdatePresence(documentId);
    }

    private (string userId, string displayName) GetUserInfo()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var displayName = Context.User?.Identity?.Name
                          ?? Context.User?.FindFirstValue("name")
                          ?? Context.User?.FindFirstValue(ClaimTypes.Email)
                          ?? "Unknown";
        return (userId, displayName);
    }

    private static string GetColorForUser(string userId)
    {
        var colors = new[]
        {
        "#ef4444", "#f97316", "#eab308", "#22c55e",
        "#3b82f6", "#6366f1", "#a855f7", "#ec4899"
        };

        var hash = Math.Abs(userId.GetHashCode());
        return colors[hash % colors.Length];
    }

    private async Task UpdatePresence(string documentId)
    {
        var users = await _sessionsStore.GetUsersAsync(documentId);

        var dto = users.Select(u => new
        {
            userId = u.UserId,
            displayName = u.DisplayName,
            color = u.Color,
            canEdit = u.CanEdit,
            isOwner = u.IsOwner
        });

        await Clients.Group(documentId).SendAsync("PresenceUpdated", dto);
    }

    private async Task<DocumentSessionUser?> GetCurrentSessionUser(string documentId)
    {
        var session = await _sessionsStore.GetOrCreateAsync(documentId);
        lock (session.ConnectedUsers)
        {
            return session.ConnectedUsers.Values.FirstOrDefault(u => u.ConnectionId == Context.ConnectionId);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var (documentId, user) = await _sessionsStore.FindByConnectionIdAsync(Context.ConnectionId);

            if (!string.IsNullOrEmpty(documentId))
            {
                await _sessionsStore.RemoveUserAsync(documentId, Context.ConnectionId);

                await UpdatePresence(documentId);
            }
        }
        catch (Exception)
        {
            throw;
        }

        await base.OnDisconnectedAsync(exception);
    }
}
