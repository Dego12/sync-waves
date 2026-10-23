using Moq;
using SyncWaves.Documents.Application.Commands;
using SyncWaves.Documents.Application.Handlers.Commands;
using SyncWaves.Documents.Domain.Abstractions;

namespace SyncWaves.Documents.Tests.Handlers;

public class ExportPdfCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenTitleContainsInvalidFileNameChars_StripsInvalidChars_AndReturnsPdfDto()
    {
        //Arrange
        var generator = new Mock<IDocumentPdfGenerator>(MockBehavior.Strict);
        var handler = new ExportPdfCommandHandler(generator.Object);

        var cmd = new ExportPdfCommand
        {
            Title = "my:doc*name",
            Content = "content"
        };

        var bytes = new byte[] { 1, 2, 3 };

        generator
            .Setup(g => g.GenerateAsync(cmd.Title, cmd.Content, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bytes);

        //Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        //Assert
        generator.Verify(g => g.GenerateAsync(cmd.Title, cmd.Content, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Same(bytes, result.PdfBytes);
        Assert.Equal("mydocname.pdf", result.FileName);
    }

    [Fact]
    public async Task Handle_WhenTitleIsEmpty_UsesDocumentFileName()
    {
        //Arrange
        var generator = new Mock<IDocumentPdfGenerator>(MockBehavior.Strict);
        var handler = new ExportPdfCommandHandler(generator.Object);

        var cmd = new ExportPdfCommand
        {
            Title = string.Empty,
            Content = "content"
        };

        var bytes = new byte[] { 9, 9, 9 };

        generator
            .Setup(g => g.GenerateAsync(cmd.Title, cmd.Content, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bytes);

        //Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        //Assert
        generator.Verify(g => g.GenerateAsync(cmd.Title, cmd.Content, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Same(bytes, result.PdfBytes);
        Assert.Equal("document.pdf", result.FileName);
    }
}
