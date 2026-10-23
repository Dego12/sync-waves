using Moq;
using SyncWaves.Documents.Application.Handlers.Queries;
using SyncWaves.Documents.Application.Queries;
using SyncWaves.Documents.Domain.Abstractions;
using SyncWaves.Documents.Domain.Entities;
using SyncWaves.Documents.Domain.Exceptions;

namespace SyncWaves.Documents.Tests.Queries;

public class GetDocumentQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenDocumentExists_ReturnsDto()
    {
        //Arrange
        var repo = new Mock<IDocumentsRepository>(MockBehavior.Strict);
        var handler = new GetDocumentQueryHandler(repo.Object);

        var doc = new DocumentEntity("Title", "Content", Guid.NewGuid()) { Version = 7 };

        var query = new GetDocumentQuery { Id = doc.Id };
        var ct = new CancellationTokenSource().Token;

        repo.Setup(r => r.GetByIdAsync(query.Id, ct))
            .ReturnsAsync(doc);

        //Act
        var result = await handler.Handle(query, ct);

        //Assert
        Assert.NotNull(result);
        Assert.Equal(doc.Id, result!.Id);
        Assert.Equal(doc.Title, result.Title);
        Assert.Equal(doc.Content, result.Content);
        Assert.Equal(doc.UserId, result.UserId);
        Assert.Equal(doc.Version, result.Version);

        repo.Verify(r => r.GetByIdAsync(query.Id, ct), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDocumentDoesNotExist_ThrowsDocumentNotFoundException()
    {
        //Arrange
        var repo = new Mock<IDocumentsRepository>(MockBehavior.Strict);
        var handler = new GetDocumentQueryHandler(repo.Object);

        var query = new GetDocumentQuery { Id = Guid.NewGuid() };

        repo.Setup(r => r.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentEntity?)null);

        //Act
        var ex = await Assert.ThrowsAsync<DocumentNotFoundException>(() => handler.Handle(query, CancellationToken.None));

        //Assert
        Assert.Equal("Document_Does_Not_Exist", ex.Message);
        repo.Verify(r => r.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
