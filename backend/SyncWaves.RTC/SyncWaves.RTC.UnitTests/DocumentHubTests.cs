using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SyncWaves.RTC.Api.DTO;
using SyncWaves.RTC.Api.Helpers;
using SyncWaves.RTC.Api.Hubs;
using System.Collections;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace SyncWaves.RTC.UnitTests;

public class DocumentHubTests
{
    [Fact]
    public async Task JoinDocument_WhenIdIsInvalid_ThrowsHubException()
    {
        //Arrange
        var states = new Mock<IDocumentStateStore>(MockBehavior.Strict);
        var sessions = new Mock<IDocumentSessionStore>(MockBehavior.Strict);
        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);

        var hub = CreateHub(states, sessions, httpFactory);

        //Act
        var act = () => hub.JoinDocument("not-a-guid");

        //Assert
        await Assert.ThrowsAsync<HubException>(act);
    }

    [Fact]
    public async Task JoinDocument_WhenIdIsValid_AddsToGroup_SavesSession_SendsLoaded_AndBroadcastsPresence()
    {
        //Arrange
        var documentId = Guid.NewGuid().ToString();
        var userId = "user-1";

        var states = new Mock<IDocumentStateStore>(MockBehavior.Strict);
        states.Setup(s => s.GetTitleAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        states.Setup(s => s.GetContentAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var sessions = new Mock<IDocumentSessionStore>(MockBehavior.Strict);
        sessions.Setup(s => s.GetOrCreateAsync(documentId))
            .ReturnsAsync(new DocumentSession { DocumentId = documentId });

        sessions.Setup(s => s.AddOrUpdateUserAsync(documentId, It.IsAny<DocumentSessionUser>()))
            .Returns(Task.CompletedTask);

        sessions.Setup(s => s.GetUsersAsync(documentId))
            .ReturnsAsync(new List<DocumentSessionUser>
            {
                new DocumentSessionUser
                {
                    ConnectionId = "c1",
                    UserId = userId,
                    DisplayName = "User",
                    Color = "#000",
                    CanEdit = true,
                    IsOwner = true
                }
            });

        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);

        var hub = CreateHub(states, sessions, httpFactory, userId: userId);

        //Act
        await hub.JoinDocument(documentId);

        //Assert
        hub.GroupsMock.Verify(g => g.AddToGroupAsync(hub.ConnectionId, documentId, It.IsAny<CancellationToken>()), Times.Once);

        sessions.Verify(s => s.GetOrCreateAsync(documentId), Times.Once);
        sessions.Verify(s => s.AddOrUpdateUserAsync(documentId, It.IsAny<DocumentSessionUser>()), Times.Once);
        sessions.Verify(s => s.GetUsersAsync(documentId), Times.Once);

        states.Verify(s => s.GetTitleAsync(documentId, It.IsAny<CancellationToken>()), Times.Once);
        states.Verify(s => s.GetContentAsync(documentId, It.IsAny<CancellationToken>()), Times.Once);

        hub.CallerProxyMock.Verify(p => p.SendCoreAsync(
            "DocumentLoaded",
            It.Is<object?[]>(a => a.Length == 2 && a[0] == null && a[1] == null),
            It.IsAny<CancellationToken>()), Times.Once);

        hub.GroupProxyMock.Verify(p => p.SendCoreAsync(
            "PresenceUpdated",
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()), Times.Once);

        httpFactory.Verify(f => f.CreateClient("DocumentsApi"), Times.Once);
    }

    [Fact]
    public async Task UpdateContent_WhenUserCanEdit_UpdatesStore_AndBroadcasts()
    {
        //Arrange
        var documentId = Guid.NewGuid().ToString();
        var userId = "user-1";

        var states = new Mock<IDocumentStateStore>(MockBehavior.Strict);
        states.Setup(s => s.UpdateContentAsync(documentId, "New", It.IsAny<CancellationToken>()))
            .ReturnsAsync("New");

        var session = new DocumentSession { DocumentId = documentId };
        session.ConnectedUsers["c1"] = new DocumentSessionUser
        {
            ConnectionId = "c1",
            UserId = userId,
            DisplayName = "User",
            Color = "#000",
            CanEdit = true,
            IsOwner = false
        };

        var sessions = new Mock<IDocumentSessionStore>(MockBehavior.Strict);
        sessions.Setup(s => s.GetOrCreateAsync(documentId))
            .ReturnsAsync(session);

        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);

        var hub = CreateHub(states, sessions, httpFactory, userId: userId, connectionIdOverride: "c1");

        //Act
        await hub.UpdateContent(documentId, "New");

        //Assert
        states.Verify(s => s.UpdateContentAsync(documentId, "New", It.IsAny<CancellationToken>()), Times.Once);

        hub.OthersProxyMock.Verify(p => p.SendCoreAsync(
            "ContentUpdated",
            It.Is<object?[]>(a => a.Length == 2 && (string?)a[0] == "New" && (string?)a[1] == userId),
            It.IsAny<CancellationToken>()), Times.Once);

        sessions.Verify(s => s.GetOrCreateAsync(documentId), Times.Once);
    }

    [Fact]
    public async Task UpdateContent_WhenUserCannotEdit_ThrowsHubException()
    {
        //Arrange
        var documentId = Guid.NewGuid().ToString();
        var userId = "user-1";

        var states = new Mock<IDocumentStateStore>(MockBehavior.Strict);

        var session = new DocumentSession { DocumentId = documentId };
        session.ConnectedUsers["c1"] = new DocumentSessionUser
        {
            ConnectionId = "c1",
            UserId = userId,
            DisplayName = "User",
            Color = "#000",
            CanEdit = false,
            IsOwner = false
        };

        var sessions = new Mock<IDocumentSessionStore>(MockBehavior.Strict);
        sessions.Setup(s => s.GetOrCreateAsync(documentId))
            .ReturnsAsync(session);

        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);

        var hub = CreateHub(states, sessions, httpFactory, userId: userId, connectionIdOverride: "c1");

        //Act
        var act = () => hub.UpdateContent(documentId, "New");

        //Assert
        await Assert.ThrowsAsync<HubException>(act);

        states.Verify(s => s.UpdateContentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        hub.OthersProxyMock.Verify(p => p.SendCoreAsync(
            "ContentUpdated",
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateTitle_WhenUserCanEdit_UpdatesStore_AndBroadcasts()
    {
        //Arrange
        var documentId = Guid.NewGuid().ToString();
        var userId = "user-1";

        var states = new Mock<IDocumentStateStore>(MockBehavior.Strict);
        states.Setup(s => s.UpdateTitleAsync(documentId, "New Title", It.IsAny<CancellationToken>()))
            .ReturnsAsync("New Title");

        var session = new DocumentSession { DocumentId = documentId };
        session.ConnectedUsers["c1"] = new DocumentSessionUser
        {
            ConnectionId = "c1",
            UserId = userId,
            DisplayName = "User",
            Color = "#000",
            CanEdit = true,
            IsOwner = false
        };

        var sessions = new Mock<IDocumentSessionStore>(MockBehavior.Strict);
        sessions.Setup(s => s.GetOrCreateAsync(documentId))
            .ReturnsAsync(session);

        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);

        var hub = CreateHub(states, sessions, httpFactory, userId: userId, connectionIdOverride: "c1");

        //Act
        await hub.UpdateTitle(documentId, "New Title");

        //Assert
        states.Verify(s => s.UpdateTitleAsync(documentId, "New Title", It.IsAny<CancellationToken>()), Times.Once);

        hub.OthersProxyMock.Verify(p => p.SendCoreAsync(
            "TitleUpdated",
            It.Is<object?[]>(a => a.Length == 2 && (string?)a[0] == "New Title" && (string?)a[1] == userId),
            It.IsAny<CancellationToken>()), Times.Once);

        sessions.Verify(s => s.GetOrCreateAsync(documentId), Times.Once);
    }

    [Fact]
    public async Task UpdateTitle_WhenUserCannotEdit_ThrowsHubException()
    {
        //Arrange
        var documentId = Guid.NewGuid().ToString();
        var userId = "user-1";

        var states = new Mock<IDocumentStateStore>(MockBehavior.Strict);

        var session = new DocumentSession { DocumentId = documentId };
        session.ConnectedUsers["c1"] = new DocumentSessionUser
        {
            ConnectionId = "c1",
            UserId = userId,
            DisplayName = "User",
            Color = "#000",
            CanEdit = false,
            IsOwner = false
        };

        var sessions = new Mock<IDocumentSessionStore>(MockBehavior.Strict);
        sessions.Setup(s => s.GetOrCreateAsync(documentId))
            .ReturnsAsync(session);

        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);

        var hub = CreateHub(states, sessions, httpFactory, userId: userId, connectionIdOverride: "c1");

        //Act
        var act = () => hub.UpdateTitle(documentId, "New Title");

        //Assert
        await Assert.ThrowsAsync<HubException>(act);

        states.Verify(s => s.UpdateTitleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        hub.OthersProxyMock.Verify(p => p.SendCoreAsync(
            "TitleUpdated",
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCursor_WhenUserIsInSession_BroadcastsCursorToOthers()
    {
        //Arrange
        var documentId = Guid.NewGuid().ToString();
        var userId = "user-1";

        var states = new Mock<IDocumentStateStore>(MockBehavior.Strict);
        var sessions = new Mock<IDocumentSessionStore>(MockBehavior.Strict);
        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);

        var session = new DocumentSession { DocumentId = documentId };
        session.ConnectedUsers["c1"] = new DocumentSessionUser
        {
            ConnectionId = "c1",
            UserId = userId,
            DisplayName = "User",
            Color = "#123",
            CanEdit = true,
            IsOwner = false
        };

        sessions.Setup(s => s.GetOrCreateAsync(documentId))
            .ReturnsAsync(session);

        var hub = CreateHub(states, sessions, httpFactory, userId: userId, connectionIdOverride: "c1");

        //Act
        await hub.UpdateCursor(documentId, 10, 3);

        //Assert
        sessions.Verify(s => s.GetOrCreateAsync(documentId), Times.Once);

        hub.OthersProxyMock.Verify(p => p.SendCoreAsync(
            "CursorUpdated",
            It.Is<object?[]>(a => a.Length == 1 && CursorPayloadMatches(a[0], userId, 10, 3, "#123", "User")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateCursor_WhenUserIsNotInSession_DoesNotBroadcast()
    {
        //Arrange
        var documentId = Guid.NewGuid().ToString();

        var states = new Mock<IDocumentStateStore>(MockBehavior.Strict);
        var sessions = new Mock<IDocumentSessionStore>(MockBehavior.Strict);
        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);

        var session = new DocumentSession { DocumentId = documentId };

        sessions.Setup(s => s.GetOrCreateAsync(documentId))
            .ReturnsAsync(session);

        var hub = CreateHub(states, sessions, httpFactory, userId: "user-1", connectionIdOverride: "c1");

        //Act
        await hub.UpdateCursor(documentId, 10, 3);

        //Assert
        sessions.Verify(s => s.GetOrCreateAsync(documentId), Times.Once);

        hub.OthersProxyMock.Verify(p => p.SendCoreAsync(
            "CursorUpdated",
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetEditPermission_WhenDocsApiSucceeds_UpdatesSession_AndBroadcastsPresence()
    {
        //Arrange
        var documentId = Guid.NewGuid().ToString();
        var targetUserId = "user-2";

        var states = new Mock<IDocumentStateStore>(MockBehavior.Strict);

        var sessions = new Mock<IDocumentSessionStore>(MockBehavior.Strict);
        sessions.Setup(s => s.SetUserCanEditAsync(documentId, targetUserId, true))
            .Returns(Task.CompletedTask);

        sessions.Setup(s => s.GetUsersAsync(documentId))
            .ReturnsAsync(new List<DocumentSessionUser>
            {
                new DocumentSessionUser
                {
                    ConnectionId = "c2",
                    UserId = targetUserId,
                    DisplayName = "User 2",
                    Color = "#000",
                    CanEdit = true,
                    IsOwner = false
                }
            });

        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);

        var hub = CreateHub(states, sessions, httpFactory, userId: "caller-1", connectionIdOverride: "c1");

        //Act
        await hub.SetEditPermission(documentId, targetUserId, true);

        //Assert
        httpFactory.Verify(f => f.CreateClient("DocumentsApi"), Times.Once);
        sessions.Verify(s => s.SetUserCanEditAsync(documentId, targetUserId, true), Times.Once);
        sessions.Verify(s => s.GetUsersAsync(documentId), Times.Once);

        hub.GroupProxyMock.Verify(p => p.SendCoreAsync(
            "PresenceUpdated",
            It.Is<object?[]>(a => a.Length == 1 && PresencePayloadContainsUser(a[0], targetUserId, canEdit: true)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetEditPermission_WhenDocsApiFails_ThrowsHubException()
    {
        //Arrange
        var documentId = Guid.NewGuid().ToString();
        var targetUserId = "user-2";

        var states = new Mock<IDocumentStateStore>(MockBehavior.Strict);
        var sessions = new Mock<IDocumentSessionStore>(MockBehavior.Strict);
        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);

        var failingClient = CreateDocsHttpClient(isOwner: true, canEdit: true, postStatusCode: HttpStatusCode.BadRequest, postResponseBody: "bad");

        var hub = CreateHub(states, sessions, httpFactory, userId: "caller-1", connectionIdOverride: "c1", docsClientOverride: failingClient);

        //Act
        var act = () => hub.SetEditPermission(documentId, targetUserId, true);

        //Assert
        var ex = await Assert.ThrowsAsync<HubException>(act);
        Assert.Contains("Failed to update permissions", ex.Message);

        httpFactory.Verify(f => f.CreateClient("DocumentsApi"), Times.Once);

        sessions.Verify(s => s.SetUserCanEditAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        sessions.Verify(s => s.GetUsersAsync(It.IsAny<string>()), Times.Never);

        hub.GroupProxyMock.Verify(p => p.SendCoreAsync(
            "PresenceUpdated",
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnDisconnectedAsync_WhenConnectionExists_RemovesUser_AndBroadcastsPresence()
    {
        //Arrange
        var documentId = Guid.NewGuid().ToString();
        var connectionId = "c1";

        var states = new Mock<IDocumentStateStore>(MockBehavior.Strict);

        var sessions = new Mock<IDocumentSessionStore>(MockBehavior.Strict);
        sessions.Setup(s => s.FindByConnectionIdAsync(connectionId))
            .ReturnsAsync((documentId, new DocumentSessionUser
            {
                ConnectionId = connectionId,
                UserId = "user-1",
                DisplayName = "User",
                Color = "#000",
                CanEdit = true,
                IsOwner = false
            }));

        sessions.Setup(s => s.RemoveUserAsync(documentId, connectionId))
            .Returns(Task.CompletedTask);

        sessions.Setup(s => s.GetUsersAsync(documentId))
            .ReturnsAsync(new List<DocumentSessionUser>());

        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);

        var hub = CreateHub(states, sessions, httpFactory, userId: "user-1", connectionIdOverride: connectionId);

        //Act
        await hub.OnDisconnectedAsync(null);

        //Assert
        sessions.Verify(s => s.FindByConnectionIdAsync(connectionId), Times.Once);
        sessions.Verify(s => s.RemoveUserAsync(documentId, connectionId), Times.Once);
        sessions.Verify(s => s.GetUsersAsync(documentId), Times.Once);

        hub.GroupProxyMock.Verify(p => p.SendCoreAsync(
            "PresenceUpdated",
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnDisconnectedAsync_WhenConnectionIsNotInStore_DoesNothing()
    {
        //Arrange
        var connectionId = "c1";

        var states = new Mock<IDocumentStateStore>(MockBehavior.Strict);

        var sessions = new Mock<IDocumentSessionStore>(MockBehavior.Strict);
        sessions.Setup(s => s.FindByConnectionIdAsync(connectionId))
            .ReturnsAsync(((string?)null, (DocumentSessionUser?)null));

        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);

        var hub = CreateHub(states, sessions, httpFactory, userId: "user-1", connectionIdOverride: connectionId);

        //Act
        await hub.OnDisconnectedAsync(null);

        //Assert
        sessions.Verify(s => s.FindByConnectionIdAsync(connectionId), Times.Once);
        sessions.Verify(s => s.RemoveUserAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        sessions.Verify(s => s.GetUsersAsync(It.IsAny<string>()), Times.Never);

        hub.GroupProxyMock.Verify(p => p.SendCoreAsync(
            "PresenceUpdated",
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private static bool CursorPayloadMatches(object? payload, string userId, int index, int length, string color, string displayName)
    {
        if (payload is null)
            return false;

        return PropertyEquals(payload, "userId", userId)
            && PropertyEquals(payload, "index", index)
            && PropertyEquals(payload, "length", length)
            && PropertyEquals(payload, "color", color)
            && PropertyEquals(payload, "displayName", displayName);
    }

    private static bool PresencePayloadContainsUser(object? payload, string userId, bool canEdit)
    {
        if (payload is not IEnumerable enumerable)
            return false;

        foreach (var item in enumerable)
        {
            if (item is null)
                continue;

            if (PropertyEquals(item, "userId", userId) && PropertyEquals(item, "canEdit", canEdit))
                return true;
        }

        return false;
    }

    private static bool PropertyEquals(object obj, string propertyName, object? expected)
    {
        var prop = obj.GetType().GetProperty(propertyName);
        if (prop is null)
            return false;

        var value = prop.GetValue(obj);
        return Equals(value, expected);
    }

    private static HttpClient CreateDocsHttpClient(bool isOwner, bool canEdit, HttpStatusCode postStatusCode = HttpStatusCode.NoContent, string? postResponseBody = null)
    {
        var handler = new FakeDocsHandler(isOwner, canEdit, postStatusCode, postResponseBody);
        return new HttpClient(handler) { BaseAddress = new Uri("http://documents.test") };
    }

    private sealed class FakeDocsHandler : HttpMessageHandler
    {
        private readonly bool _isOwner;
        private readonly bool _canEdit;
        private readonly HttpStatusCode _postStatusCode;
        private readonly string? _postResponseBody;

        public FakeDocsHandler(bool isOwner, bool canEdit, HttpStatusCode postStatusCode, string? postResponseBody)
        {
            _isOwner = isOwner;
            _canEdit = canEdit;
            _postStatusCode = postStatusCode;
            _postResponseBody = postResponseBody;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            if (request.Method == HttpMethod.Get && path.EndsWith("/permissions/me", StringComparison.OrdinalIgnoreCase))
            {
                var json = $"{{\"isOwner\":{_isOwner.ToString().ToLowerInvariant()},\"canEdit\":{_canEdit.ToString().ToLowerInvariant()}}}";
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
            }

            if (request.Method == HttpMethod.Post && path.Contains("/permissions", StringComparison.OrdinalIgnoreCase))
            {
                var response = new HttpResponseMessage(_postStatusCode);
                if (!string.IsNullOrEmpty(_postResponseBody))
                    response.Content = new StringContent(_postResponseBody, Encoding.UTF8, "text/plain");
                return Task.FromResult(response);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    private static TestableDocumentHub CreateHub(
        Mock<IDocumentStateStore> store,
        Mock<IDocumentSessionStore> sessionStore,
        Mock<IHttpClientFactory>? httpClientFactoryMock = null,
        string? userId = null,
        string? connectionIdOverride = null,
        bool docsIsOwner = true,
        bool docsCanEdit = true,
        HttpClient? docsClientOverride = null)
    {
        var docsClient = docsClientOverride ?? CreateDocsHttpClient(docsIsOwner, docsCanEdit);

        IHttpClientFactory httpClientFactory;
        if (httpClientFactoryMock is not null)
        {
            httpClientFactoryMock.Setup(f => f.CreateClient("DocumentsApi")).Returns(docsClient);
            httpClientFactory = httpClientFactoryMock.Object;
        }
        else
        {
            httpClientFactory = new SingleClientFactory(docsClient);
        }

        var hub = new TestableDocumentHub(store.Object, sessionStore.Object, httpClientFactory);

        var callerProxy = new Mock<ISingleClientProxy>(MockBehavior.Strict);
        callerProxy.Setup(p => p.SendCoreAsync("DocumentLoaded", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var othersProxy = new Mock<IClientProxy>(MockBehavior.Strict);
        othersProxy.Setup(p => p.SendCoreAsync("ContentUpdated", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        othersProxy.Setup(p => p.SendCoreAsync("TitleUpdated", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        othersProxy.Setup(p => p.SendCoreAsync("CursorUpdated", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var groupProxy = new Mock<IClientProxy>(MockBehavior.Strict);
        groupProxy.Setup(p => p.SendCoreAsync("PresenceUpdated", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var clients = new Mock<IHubCallerClients>(MockBehavior.Strict);
        clients.Setup(c => c.Caller).Returns(callerProxy.Object);
        clients.Setup(c => c.OthersInGroup(It.IsAny<string>())).Returns(othersProxy.Object);
        clients.Setup(c => c.Group(It.IsAny<string>())).Returns(groupProxy.Object);

        var groups = new Mock<IGroupManager>(MockBehavior.Strict);
        groups.Setup(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var connectionId = connectionIdOverride ?? Guid.NewGuid().ToString();

        var claims = new List<Claim>();
        if (userId is not null)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

        var services = new ServiceCollection();
        services.AddSingleton<IHttpClientFactory>(httpClientFactory);
        var sp = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = sp;

        var features = new FeatureCollection();
        features.Set<IHttpContextFeature>(new TestHttpContextFeature { HttpContext = httpContext });

        var context = new Mock<HubCallerContext>(MockBehavior.Strict);
        context.SetupGet(c => c.ConnectionId).Returns(connectionId);
        context.SetupGet(c => c.User).Returns(principal);
        context.SetupGet(c => c.Features).Returns(features);

        hub.SetTestContext(context.Object, clients.Object, groups.Object, callerProxy, othersProxy, groupProxy, connectionId);

        return hub;
    }

    private sealed class TestHttpContextFeature : IHttpContextFeature
    {
        public HttpContext HttpContext { get; set; } = default!;
    }

    private sealed class SingleClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;
        public SingleClientFactory(HttpClient client) => _client = client;
        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class TestableDocumentHub : DocumentHub
    {
        public TestableDocumentHub(IDocumentStateStore store, IDocumentSessionStore sessionStore, IHttpClientFactory httpClientFactory)
            : base(store, sessionStore, httpClientFactory) { }

        public Mock<IGroupManager> GroupsMock { get; private set; } = null!;
        public Mock<ISingleClientProxy> CallerProxyMock { get; private set; } = null!;
        public Mock<IClientProxy> OthersProxyMock { get; private set; } = null!;
        public Mock<IClientProxy> GroupProxyMock { get; private set; } = null!;
        public string ConnectionId { get; private set; } = string.Empty;

        public void SetTestContext(
            HubCallerContext context,
            IHubCallerClients clients,
            IGroupManager groups,
            Mock<ISingleClientProxy> callerProxy,
            Mock<IClientProxy> othersProxy,
            Mock<IClientProxy> groupProxy,
            string connectionId)
        {
            Context = context;
            Clients = clients;
            Groups = groups;

            CallerProxyMock = callerProxy;
            OthersProxyMock = othersProxy;
            GroupProxyMock = groupProxy;

            GroupsMock = Mock.Get(groups);
            ConnectionId = connectionId;
        }
    }
}
