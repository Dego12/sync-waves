using Moq;
using SyncWaves.Documents.Application.Commands;
using SyncWaves.Documents.Application.Handlers.Commands;
using SyncWaves.Documents.Domain.Abstractions;
using SyncWaves.Documents.Domain.Entities;
using SyncWaves.Documents.Domain.Exceptions;

namespace SyncWaves.Documents.Tests.Handlers;

public class UpdateDocumentCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenDocumentExists_UpdatesDocument_AndCallsRepository()
    {
        //Arrange
        var repo = new Mock<IDocumentsRepository>(MockBehavior.Strict);
        var handler = new UpdateDocumentCommandHandler(repo.Object);

        var doc = new DocumentEntity("t", "c", Guid.NewGuid());

        var cmd = new UpdateDocumentCommand
        {
            DocumentId = doc.Id,
            Title = "new title",
            Content = "new content",
            Version = 7
        };

        repo.Setup(r => r.GetByIdAsync(cmd.DocumentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);

        repo.Setup(r => r.UpdateAsync(doc, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        //Act
        await handler.Handle(cmd, CancellationToken.None);

        //Assert
        Assert.Equal("new title", doc.Title);
        Assert.Equal("new content", doc.Content);
        Assert.Equal(7, doc.Version);

        repo.Verify(r => r.GetByIdAsync(cmd.DocumentId, It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.UpdateAsync(doc, It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDocumentDoesNotExist_ThrowsDocumentNotFoundException()
    {
        //Arrange
        var repo = new Mock<IDocumentsRepository>(MockBehavior.Strict);
        var handler = new UpdateDocumentCommandHandler(repo.Object);

        var cmd = new UpdateDocumentCommand
        {
            DocumentId = Guid.NewGuid(),
            Title = "new title",
            Content = "new content",
            Version = 1
        };

        repo.Setup(r => r.GetByIdAsync(cmd.DocumentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentEntity?)null);

        //Act
        var ex = await Assert.ThrowsAsync<DocumentNotFoundException>(() => handler.Handle(cmd, CancellationToken.None));

        //Assert
        Assert.Equal("Document_Does_Not_Exist", ex.Message);

        repo.Verify(r => r.GetByIdAsync(cmd.DocumentId, It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.UpdateAsync(It.IsAny<DocumentEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
