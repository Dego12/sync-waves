using Moq;
using SyncWaves.Documents.Application.Handlers.Queries;
using SyncWaves.Documents.Application.Queries;
using SyncWaves.Documents.Domain.Abstractions;
using SyncWaves.Documents.Domain.Entities;

namespace SyncWaves.Documents.Tests.Queries;

public class GetPermissionsByUserIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenRepositoryReturnsEntity_MapsToDto()
    {
        //Arrange
        var repo = new Mock<IDocumentPermissionsRepository>(MockBehavior.Strict);
        var handler = new GetPermissionsByUserIdQueryHandler(repo.Object);

        var query = new GetPermissionsByUserIdQuery
        {
            DocumentId = Guid.NewGuid(),
            UserId = Guid.NewGuid()
        };

        repo.Setup(r => r.GetPermissionsForUserAsync(query.DocumentId, query.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DocumentCollaboratorEntity
            {
                DocumentId = query.DocumentId,
                UserId = query.UserId,
                IsOwner = false,
                CanEdit = true
            });

        //Act
        var dto = await handler.Handle(query, CancellationToken.None);

        //Assert
        Assert.False(dto.IsOwner);
        Assert.True(dto.CanEdit);

        repo.Verify(r => r.GetPermissionsForUserAsync(query.DocumentId, query.UserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrowsArgumentException_PropagatesException()
    {
        //Arrange
        var repo = new Mock<IDocumentPermissionsRepository>(MockBehavior.Strict);
        var handler = new GetPermissionsByUserIdQueryHandler(repo.Object);

        var query = new GetPermissionsByUserIdQuery
        {
            DocumentId = Guid.NewGuid(),
            UserId = Guid.NewGuid()
        };

        repo.Setup(r => r.GetPermissionsForUserAsync(query.DocumentId, query.UserId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Document_Not_Found"));

        //Act
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(query, CancellationToken.None));

        //Assert
        Assert.Equal("Document_Not_Found", ex.Message);
    }
}
