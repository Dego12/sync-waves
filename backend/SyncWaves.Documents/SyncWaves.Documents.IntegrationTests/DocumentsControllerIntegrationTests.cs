using System.Net;
using System.Net.Http.Json;
using SyncWaves.Documents.Application.Commands;
using SyncWaves.Documents.Application.DTOs;
using SyncWaves.Documents.Domain.Entities;

namespace SyncWaves.Documents.IntegrationTests;

public class DocumentsControllerIntegrationTests : IClassFixture<DocumentsApiFixture>
{
    private readonly DocumentsApiFixture _fx;

    public DocumentsControllerIntegrationTests(DocumentsApiFixture fx)
    {
        _fx = fx;
        _fx.Documents.Clear();
        _fx.Pdfs.Clear();
    }

    [Fact]
    public async Task Create_WhenCalled_ReturnsOk_AndPersistsDocument_AndSetsOwnerPermissions()
    {
        //Arrange
        var userId = Guid.NewGuid();
        SetUser(userId);

        var cmd = new CreateDocumentCommand
        {
            Title = "My doc",
            Content = "Hello",
            UserId = Guid.Empty
        };

        //Act
        var resp = await _fx.Client.PostAsJsonAsync("/Documents", cmd);

        //Assert
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var dto = await resp.Content.ReadFromJsonAsync<DocumentDTO>();
        Assert.NotNull(dto);
        Assert.Equal("My doc", dto!.Title);
        Assert.Equal("Hello", dto.Content);
        Assert.Equal(userId, dto.UserId);

        var stored = await _fx.Documents.GetByIdAsync(dto.Id);
        Assert.NotNull(stored);
        Assert.Equal(userId, stored!.UserId);

        var perms = await _fx.Collaborators.GetPermissionsForUserAsync(dto.Id, userId);
        Assert.True(perms.IsOwner);
        Assert.True(perms.CanEdit);
    }

    [Fact]
    public async Task GetMine_WhenUserHasDocuments_ReturnsOnlyCurrentUserDocuments()
    {
        //Arrange
        var me = Guid.NewGuid();
        var other = Guid.NewGuid();

        _fx.Documents.Seed(new DocumentEntity("Mine 1", "A", me));
        _fx.Documents.Seed(new DocumentEntity("Mine 2", "B", me));
        _fx.Documents.Seed(new DocumentEntity("Other", "X", other));

        SetUser(me);

        //Act
        var resp = await _fx.Client.GetAsync("/Documents/mine");

        //Assert
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var docs = await resp.Content.ReadFromJsonAsync<List<DocumentDTO>>();
        Assert.NotNull(docs);
        Assert.Equal(2, docs!.Count);
        Assert.All(docs, d => Assert.Equal(me, d.UserId));
    }

    [Fact]
    public async Task GetMine_WhenUserHasNoDocuments_ReturnsNoContent()
    {
        //Arrange
        SetUser(Guid.NewGuid());

        //Act
        var resp = await _fx.Client.GetAsync("/Documents/mine");

        //Assert
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    [Fact]
    public async Task GetById_WhenMissing_ReturnsNoContent()
    {
        //Arrange
        var id = Guid.NewGuid();

        //Act
        var resp = await _fx.Client.GetAsync($"/Documents/{id}");

        //Assert
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    [Fact]
    public async Task GetById_WhenExists_ReturnsOkAndDocument()
    {
        //Arrange
        var ownerId = Guid.NewGuid();
        var doc = new DocumentEntity("Title", "Content", ownerId);
        _fx.Documents.Seed(doc);

        //Act
        var resp = await _fx.Client.GetAsync($"/Documents/{doc.Id}");

        //Assert
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var dto = await resp.Content.ReadFromJsonAsync<DocumentDTO>();
        Assert.NotNull(dto);
        Assert.Equal(doc.Id, dto!.Id);
        Assert.Equal("Title", dto.Title);
        Assert.Equal("Content", dto.Content);
        Assert.Equal(ownerId, dto.UserId);
    }

    [Fact]
    public async Task Update_WhenMissing_ReturnsNoContent()
    {
        //Arrange
        SetUser(Guid.NewGuid());

        var cmd = new UpdateDocumentCommand
        {
            DocumentId = Guid.NewGuid(),
            Title = "New",
            Content = "Updated",
            Version = 1
        };

        //Act
        var resp = await _fx.Client.PutAsJsonAsync("/Documents", cmd);

        //Assert
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    [Fact]
    public async Task Update_WhenExists_ReturnsOk_AndUpdatesStoredDocument()
    {
        //Arrange
        var ownerId = Guid.NewGuid();
        var doc = new DocumentEntity("Old", "Old content", ownerId);
        _fx.Documents.Seed(doc);

        SetUser(ownerId);

        var cmd = new UpdateDocumentCommand
        {
            DocumentId = doc.Id,
            Title = "New",
            Content = "Updated",
            Version = 5
        };

        //Act
        var resp = await _fx.Client.PutAsJsonAsync("/Documents", cmd);

        //Assert
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var stored = await _fx.Documents.GetByIdAsync(doc.Id);
        Assert.NotNull(stored);
        Assert.Equal("New", stored!.Title);
        Assert.Equal("Updated", stored.Content);
        Assert.Equal(5, stored.Version);
    }

    [Fact]
    public async Task ExportPdf_WhenCalled_ReturnsPdfFile()
    {
        //Arrange
        var cmd = new ExportPdfCommand
        {
            Title = "Report",
            Content = "Content"
        };

        //Act
        var resp = await _fx.Client.PostAsJsonAsync("/Documents/export/pdf", cmd);

        //Assert
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("application/pdf", resp.Content.Headers.ContentType?.MediaType);

        var bytes = await resp.Content.ReadAsByteArrayAsync();
        Assert.NotEmpty(bytes);

        Assert.Single(_fx.Pdfs.Requests);
        Assert.Equal("Report", _fx.Pdfs.Requests[0].Title);
        Assert.Equal("Content", _fx.Pdfs.Requests[0].Content);

        Assert.NotNull(resp.Content.Headers.ContentDisposition);
        Assert.Equal("attachment", resp.Content.Headers.ContentDisposition!.DispositionType);
        Assert.Equal("Report.pdf", resp.Content.Headers.ContentDisposition.FileName?.Trim('"'));
    }

    [Fact]
    public async Task GetMyPermissions_WhenOwnerRequests_ReturnsIsOwnerTrueAndCanEditTrue()
    {
        //Arrange
        var ownerId = Guid.NewGuid();
        var doc = new DocumentEntity("Doc", "Content", ownerId);
        _fx.Documents.Seed(doc);

        SetUser(ownerId);

        //Act
        var resp = await _fx.Client.GetAsync($"/Documents/{doc.Id}/permissions/me");

        //Assert
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var dto = await resp.Content.ReadFromJsonAsync<DocumentPermissionsDTO>();
        Assert.NotNull(dto);
        Assert.True(dto!.IsOwner);
        Assert.True(dto.CanEdit);
    }

    [Fact]
    public async Task SetPermission_WhenOwnerSetsPermission_UpdatesCollaboratorPermissions()
    {
        //Arrange
        var ownerId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var doc = new DocumentEntity("Doc", "Content", ownerId);
        _fx.Documents.Seed(doc);

        SetUser(ownerId);

        //Act
        var setResp = await _fx.Client.PostAsJsonAsync(
            $"/Documents/{doc.Id}/permissions",
            new UpdatePermissionDTO { UserId = targetUserId, CanEdit = true });

        //Assert
        Assert.Equal(HttpStatusCode.NoContent, setResp.StatusCode);

        SetUser(targetUserId);

        var getResp = await _fx.Client.GetAsync($"/Documents/{doc.Id}/permissions/me");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);

        var dto = await getResp.Content.ReadFromJsonAsync<DocumentPermissionsDTO>();
        Assert.NotNull(dto);
        Assert.False(dto!.IsOwner);
        Assert.True(dto.CanEdit);
    }

    [Fact]
    public async Task SetPermission_WhenNonOwnerSetsPermission_ReturnsForbidden()
    {
        //Arrange
        var ownerId = Guid.NewGuid();
        var nonOwnerId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var doc = new DocumentEntity("Doc", "Content", ownerId);
        _fx.Documents.Seed(doc);

        SetUser(nonOwnerId);

        //Act
        var resp = await _fx.Client.PostAsJsonAsync(
            $"/Documents/{doc.Id}/permissions",
            new UpdatePermissionDTO { UserId = targetUserId, CanEdit = true });

        //Assert
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    private void SetUser(Guid userId)
    {
        _fx.Client.DefaultRequestHeaders.Remove(TestAuthHandler.UserIdHeader);
        _fx.Client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
    }
}
