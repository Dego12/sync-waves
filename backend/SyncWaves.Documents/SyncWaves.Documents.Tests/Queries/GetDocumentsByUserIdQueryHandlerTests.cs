using Moq;
using SyncWaves.Documents.Application.Handlers.Queries;
using SyncWaves.Documents.Application.Queries;
using SyncWaves.Documents.Domain.Abstractions;
using SyncWaves.Documents.Domain.Entities;
using SyncWaves.Documents.Domain.Exceptions;

namespace SyncWaves.Documents.Tests.Queries;

public class GetDocumentsByUserIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenDocumentsExist_ReturnsDtos()
    {
        //Arrange
        var repo = new Mock<IDocumentsRepository>(MockBehavior.Strict);
        var handler = new GetDocumentsByUserIdQueryHandler(repo.Object);

        var userId = Guid.NewGuid();
        var docs = new List<DocumentEntity>
        {
            new("t1", "c1", userId),
            new("t2", "c2", userId)
        };

        var query = new GetDocumentsByUserIdQuery { UserId = userId };

        repo.Setup(r => r.GetByUserId(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(docs);

        //Act
        var result = await handler.Handle(query, CancellationToken.None);

        //Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(docs[0].Id, result[0].Id);
        Assert.Equal(docs[1].Id, result[1].Id);
        Assert.All(result, d => Assert.Equal(userId, d.UserId));

        repo.Verify(r => r.GetByUserId(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoDocuments_ThrowsDocumentNotFoundException()
    {
        //Arrange
        var repo = new Mock<IDocumentsRepository>(MockBehavior.Strict);
        var handler = new GetDocumentsByUserIdQueryHandler(repo.Object);

        var userId = Guid.NewGuid();
        var query = new GetDocumentsByUserIdQuery { UserId = userId };

        repo.Setup(r => r.GetByUserId(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentEntity>());

        //Act
        var ex = await Assert.ThrowsAsync<DocumentNotFoundException>(() => handler.Handle(query, CancellationToken.None));

        //Assert
        Assert.Equal("No_Documents", ex.Message);
        repo.Verify(r => r.GetByUserId(userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
