using Moq;
using SyncWaves.Documents.Application.Commands;
using SyncWaves.Documents.Application.Handlers.Commands;
using SyncWaves.Documents.Domain.Abstractions;
using SyncWaves.Documents.Domain.Exceptions;

namespace SyncWaves.Documents.Tests.Handlers;

public class UpdatePermissionCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenCalled_DelegatesToRepository()
    {
        //Arrange
        var repo = new Mock<IDocumentPermissionsRepository>(MockBehavior.Strict);
        var handler = new UpdatePermissionCommandHandler(repo.Object);

        var cmd = new UpdatePermissionCommand
        {
            DocumentId = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CanEdit = true
        };

        repo.Setup(r => r.SetEditPermissionAsync(cmd.DocumentId, cmd.OwnerId, cmd.UserId, cmd.CanEdit, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        //Act
        await handler.Handle(cmd, CancellationToken.None);

        //Assert
        repo.Verify(r => r.SetEditPermissionAsync(cmd.DocumentId, cmd.OwnerId, cmd.UserId, cmd.CanEdit, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrowsDocumentNotFoundException_PropagatesException()
    {
        //Arrange
        var repo = new Mock<IDocumentPermissionsRepository>(MockBehavior.Strict);
        var handler = new UpdatePermissionCommandHandler(repo.Object);

        var cmd = new UpdatePermissionCommand
        {
            DocumentId = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CanEdit = true
        };

        repo.Setup(r => r.SetEditPermissionAsync(cmd.DocumentId, cmd.OwnerId, cmd.UserId, cmd.CanEdit, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DocumentNotFoundException("Document_Not_Found"));

        //Act
        var ex = await Assert.ThrowsAsync<DocumentNotFoundException>(() => handler.Handle(cmd, CancellationToken.None));

        //Assert
        Assert.Equal("Document_Not_Found", ex.Message);
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrowsUnauthorizedAccessException_PropagatesException()
    {
        //Arrange
        var repo = new Mock<IDocumentPermissionsRepository>(MockBehavior.Strict);
        var handler = new UpdatePermissionCommandHandler(repo.Object);

        var cmd = new UpdatePermissionCommand
        {
            DocumentId = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CanEdit = true
        };

        repo.Setup(r => r.SetEditPermissionAsync(cmd.DocumentId, cmd.OwnerId, cmd.UserId, cmd.CanEdit, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Only_Owner_Changes"));

        //Act
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(cmd, CancellationToken.None));

        //Assert
        Assert.Equal("Only_Owner_Changes", ex.Message);
    }
}
