using Moq;
using SyncWaves.Documents.Application.Commands;
using SyncWaves.Documents.Application.Handlers.Commands;
using SyncWaves.Documents.Domain.Abstractions;
using SyncWaves.Documents.Domain.Entities;

namespace SyncWaves.Documents.Tests.Handlers;

public class CreateDocumentCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenTitleHasWhitespace_TrimsTitle_AndReturnsDto()
    {
        //Arrange
        var repo = new Mock<IDocumentsRepository>(MockBehavior.Strict);
        var handler = new CreateDocumentCommandHandler(repo.Object);

        var cmd = new CreateDocumentCommand
        {
            Title = "  Untitled  ",
            Content = "Hello",
            UserId = Guid.NewGuid()
        };

        DocumentEntity? added = null;

        repo.Setup(r => r.AddAsync(It.IsAny<DocumentEntity>(), It.IsAny<CancellationToken>()))
            .Callback<DocumentEntity, CancellationToken>((d, _) => added = d)
            .Returns(Task.CompletedTask);

        repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        //Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        //Assert
        repo.Verify(r => r.AddAsync(It.IsAny<DocumentEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.NotNull(added);
        Assert.Equal("Untitled", added!.Title);
        Assert.Equal(cmd.Content, added.Content);
        Assert.Equal(cmd.UserId, added.UserId);

        Assert.Equal(added.Id, result.Id);
        Assert.Equal(added.Title, result.Title);
        Assert.Equal(added.Content, result.Content);
        Assert.Equal(added.UserId, result.UserId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenTitleIsEmpty_UsesUntitled(string title)
    {
        //Arrange
        var repo = new Mock<IDocumentsRepository>(MockBehavior.Strict);
        var handler = new CreateDocumentCommandHandler(repo.Object);

        var cmd = new CreateDocumentCommand
        {
            Title = title,
            Content = "Hello",
            UserId = Guid.NewGuid()
        };

        DocumentEntity? added = null;

        repo.Setup(r => r.AddAsync(It.IsAny<DocumentEntity>(), It.IsAny<CancellationToken>()))
            .Callback<DocumentEntity, CancellationToken>((d, _) => added = d)
            .Returns(Task.CompletedTask);

        repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        //Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        //Assert
        Assert.NotNull(added);
        Assert.Equal("Untitled", added!.Title);
        Assert.Equal("Untitled", result.Title);
    }
}
