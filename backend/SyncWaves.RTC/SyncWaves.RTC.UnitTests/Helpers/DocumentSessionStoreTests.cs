using SyncWaves.RTC.Api.DTO;
using SyncWaves.RTC.Api.Helpers;

namespace SyncWaves.RTC.UnitTests.Helpers;

public class DocumentSessionStoreTests
{
    [Fact]
    public async Task GetOrCreate_WhenCalledTwice_ReturnsSameInstance()
    {
        //Arrange
        var store = new DocumentSessionStore();
        var documentId = "doc-1";

        //Act
        var s1 = await store.GetOrCreateAsync(documentId);
        var s2 = await store.GetOrCreateAsync(documentId);

        //Assert
        Assert.Same(s1, s2);
        Assert.Equal(documentId, s1.DocumentId);
    }

    [Fact]
    public async Task GetUsers_WhenSessionDoesNotExist_ReturnsEmpty()
    {
        //Arrange
        var store = new DocumentSessionStore();
        var documentId = "doc-1";

        //Act
        var users = await store.GetUsersAsync(documentId);

        //Assert
        Assert.Empty(users);
    }

    [Fact]
    public async Task AddOrUpdateUser_ThenGetUsers_ReturnsUser()
    {
        //Arrange
        var store = new DocumentSessionStore();
        var documentId = "doc-1";

        var user = new DocumentSessionUser
        {
            ConnectionId = "c1",
            UserId = "u1",
            DisplayName = "User",
            Color = "#000",
            CanEdit = true,
            IsOwner = true
        };

        //Act
        await store.AddOrUpdateUserAsync(documentId, user);
        var users = await store.GetUsersAsync(documentId);

        //Assert
        var saved = Assert.Single(users);
        Assert.Equal("c1", saved.ConnectionId);
        Assert.Equal("u1", saved.UserId);
    }

    [Fact]
    public async Task AddOrUpdateUser_WhenSameConnectionExists_OverwritesUser()
    {
        //Arrange
        var store = new DocumentSessionStore();
        var documentId = "doc-1";

        await store.AddOrUpdateUserAsync(documentId, new DocumentSessionUser
        {
            ConnectionId = "c1",
            UserId = "u1",
            DisplayName = "User",
            Color = "#000",
            CanEdit = false,
            IsOwner = false
        });

        var updated = new DocumentSessionUser
        {
            ConnectionId = "c1",
            UserId = "u2",
            DisplayName = "User 2",
            Color = "#111",
            CanEdit = true,
            IsOwner = true
        };

        //Act
        await store.AddOrUpdateUserAsync(documentId, updated);
        var users = await store.GetUsersAsync(documentId);

        //Assert
        var saved = Assert.Single(users);
        Assert.Equal("u2", saved.UserId);
        Assert.Equal("User 2", saved.DisplayName);
        Assert.True(saved.CanEdit);
        Assert.True(saved.IsOwner);
    }

    [Fact]
    public async Task RemoveUser_WhenUserExists_RemovesFromSession()
    {
        //Arrange
        var store = new DocumentSessionStore();
        var documentId = "doc-1";

        await store.AddOrUpdateUserAsync(documentId, new DocumentSessionUser
        {
            ConnectionId = "c1",
            UserId = "u1",
            DisplayName = "User",
            Color = "#000",
            CanEdit = true,
            IsOwner = true
        });

        //Act
        await store.RemoveUserAsync(documentId, "c1");
        var users = await store.GetUsersAsync(documentId);

        //Assert
        Assert.Empty(users);
    }

    [Fact]
    public async Task SetUserCanEdit_WhenUserHasMultipleConnections_UpdatesAllConnections()
    {
        //Arrange
        var store = new DocumentSessionStore();
        var documentId = "doc-1";

        await store.AddOrUpdateUserAsync(documentId, new DocumentSessionUser
        {
            ConnectionId = "c1",
            UserId = "u1",
            DisplayName = "User",
            Color = "#000",
            CanEdit = false,
            IsOwner = false
        });

        await store.AddOrUpdateUserAsync(documentId, new DocumentSessionUser
        {
            ConnectionId = "c2",
            UserId = "u1",
            DisplayName = "User",
            Color = "#000",
            CanEdit = false,
            IsOwner = false
        });

        //Act
        await store.SetUserCanEditAsync(documentId, "u1", true);
        var users = await store.GetUsersAsync(documentId);

        //Assert
        Assert.All(users, u => Assert.True(u.CanEdit));
    }

    [Fact]
    public async Task FindByConnectionId_WhenConnectionExists_ReturnsDocumentAndUser()
    {
        //Arrange
        var store = new DocumentSessionStore();
        var documentId = "doc-1";

        var user = new DocumentSessionUser
        {
            ConnectionId = "c1",
            UserId = "u1",
            DisplayName = "User",
            Color = "#000",
            CanEdit = true,
            IsOwner = false
        };

        await store.AddOrUpdateUserAsync(documentId, user);

        //Act
        var result = await store.FindByConnectionIdAsync("c1");

        //Assert
        Assert.Equal(documentId, result.DocumentId);
        Assert.NotNull(result.User);
        Assert.Equal("c1", result.User!.ConnectionId);
        Assert.Equal("u1", result.User!.UserId);
    }

    [Fact]
    public async Task FindByConnectionId_WhenConnectionDoesNotExist_ReturnsNulls()
    {
        //Arrange
        var store = new DocumentSessionStore();

        //Act
        var result = await store.FindByConnectionIdAsync("missing");

        //Assert
        Assert.Null(result.DocumentId);
        Assert.Null(result.User);
    }
}
