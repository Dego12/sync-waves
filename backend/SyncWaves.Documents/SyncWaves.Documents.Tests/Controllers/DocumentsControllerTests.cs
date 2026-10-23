using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SyncWaves.Documents.Api.Controllers;
using SyncWaves.Documents.Api.Responses;
using SyncWaves.Documents.Application.Commands;
using SyncWaves.Documents.Application.DTOs;
using SyncWaves.Documents.Application.Queries;
using SyncWaves.Documents.Domain.Entities;
using SyncWaves.Documents.Domain.Exceptions;
using System.Security.Claims;

namespace SyncWaves.Documents.Tests.Controllers;

public class DocumentsControllerTests
{
    [Fact]
    public async Task GetById_WhenDocumentExists_ReturnsOkObjectResult()
    {
        //Arrange
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        var documentId = Guid.NewGuid();

        var dto = new DocumentDTO
        {
            Id = documentId,
            Title = "t",
            Content = "c",
            UserId = Guid.NewGuid(),
            Version = 3
        };

        mediator
            .Setup(m => m.Send(It.Is<GetDocumentQuery>(q => q.Id == documentId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var sut = CreateSut(mediator);

        //Act
        var result = await sut.GetById(documentId);

        //Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(dto, ok.Value);

        mediator.Verify(m => m.Send(It.Is<GetDocumentQuery>(q => q.Id == documentId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMine_WhenUserHasIdClaim_SendsQueryWithUserId_AndReturnsOkObjectResult()
    {
        //Arrange
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        var userId = Guid.NewGuid();

        var docs = new List<DocumentDTO>
        {
            new() { Id = Guid.NewGuid(), Title = "t1", Content = "c1", UserId = userId },
            new() { Id = Guid.NewGuid(), Title = "t2", Content = "c2", UserId = userId }
        };

        mediator
            .Setup(m => m.Send(It.Is<GetDocumentsByUserIdQuery>(q => q.UserId == userId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(docs);

        var sut = CreateSut(mediator, CreateUser(userId));

        //Act
        var result = await sut.GetMine(CancellationToken.None);

        //Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(docs, ok.Value);

        mediator.Verify(m => m.Send(It.Is<GetDocumentsByUserIdQuery>(q => q.UserId == userId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WhenCalled_SetsUserId_SetsOwnerPermission_AndReturnsOkObjectResult()
    {
        //Arrange
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        var userId = Guid.NewGuid();

        var command = new CreateDocumentCommand { Title = "t", Content = "c" };

        var dto = new DocumentDTO
        {
            Id = Guid.NewGuid(),
            Title = "t",
            Content = "c",
            UserId = userId
        };

        mediator
            .Setup(m => m.Send(It.IsAny<CreateDocumentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        mediator
            .Setup(m => m.Send(It.IsAny<UpdatePermissionCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut(mediator, CreateUser(userId));

        //Act
        var result = await sut.Create(command);

        //Assert
        mediator.Verify(m => m.Send(It.Is<CreateDocumentCommand>(c => c.UserId == userId), It.IsAny<CancellationToken>()), Times.Once);

        mediator.Verify(m => m.Send(It.Is<UpdatePermissionCommand>(c =>
            c.OwnerId == userId &&
            c.UserId == userId &&
            c.CanEdit &&
            c.DocumentId == dto.Id
        ), It.IsAny<CancellationToken>()), Times.Once);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(dto, ok.Value);
    }

    [Fact]
    public async Task Update_WhenMediatorSucceeds_ReturnsOkResult()
    {
        //Arrange
        var mediator = new Mock<IMediator>(MockBehavior.Strict);

        var command = new UpdateDocumentCommand
        {
            DocumentId = Guid.NewGuid(),
            Title = "new",
            Content = "content",
            Version = 7
        };

        mediator
            .Setup(m => m.Send(It.IsAny<UpdateDocumentCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut(mediator, CreateUser(Guid.NewGuid()));

        //Act
        var result = await sut.Update(command);

        //Assert
        Assert.IsType<OkResult>(result);

        mediator.Verify(m => m.Send(It.Is<UpdateDocumentCommand>(c => c.DocumentId == command.DocumentId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_WhenMediatorThrowsDocumentConflictException_ReturnsConflictObjectResultWithCurrentDocument()
    {
        //Arrange
        var mediator = new Mock<IMediator>(MockBehavior.Strict);

        var current = new DocumentEntity("Current title", "Current content", Guid.NewGuid())
        {
            Version = 11,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc)
        };

        var command = new UpdateDocumentCommand
        {
            DocumentId = current.Id,
            Title = "new",
            Content = "content",
            Version = 10
        };

        mediator
            .Setup(m => m.Send(It.IsAny<UpdateDocumentCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DocumentConflictException(current));

        var sut = CreateSut(mediator, CreateUser(Guid.NewGuid()));

        //Act
        var result = await sut.Update(command);

        //Assert
        var conflict = Assert.IsType<ConflictObjectResult>(result);
        var payload = Assert.IsType<DocumentConflictResponse>(conflict.Value);

        Assert.NotNull(payload.Current);
        Assert.Equal(current.Id, payload.Current!.Id);
        Assert.Equal(current.Title, payload.Current.Title);
        Assert.Equal(current.Content, payload.Current.Content);
        Assert.Equal(current.Version, payload.Current.Version);
        Assert.Equal(current.CreatedAt, payload.Current.CreatedAt);
        Assert.Equal(current.UpdatedAt, payload.Current.UpdatedAt);
        Assert.Equal(current.UserId, payload.Current.UserId);

        mediator.Verify(m => m.Send(It.IsAny<UpdateDocumentCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExportPdf_WhenPdfBytesAreReturned_ReturnsFileContentResult()
    {
        //Arrange
        var mediator = new Mock<IMediator>(MockBehavior.Strict);

        var command = new ExportPdfCommand
        {
            Title = "Report",
            Content = "Content"
        };

        var bytes = new byte[] { 1, 2, 3 };

        mediator
            .Setup(m => m.Send(It.IsAny<ExportPdfCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PdfDTO { PdfBytes = bytes, FileName = "Report.pdf" });

        var sut = CreateSut(mediator, CreateUser(Guid.NewGuid()));

        //Act
        var result = await sut.ExportPdf(command);

        //Assert
        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal("Report.pdf", file.FileDownloadName);
        Assert.Same(bytes, file.FileContents);

        mediator.Verify(m => m.Send(It.IsAny<ExportPdfCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExportPdf_WhenPdfBytesAreNull_ReturnsNotFoundObjectResult()
    {
        //Arrange
        var mediator = new Mock<IMediator>(MockBehavior.Strict);

        var command = new ExportPdfCommand
        {
            Title = "Report",
            Content = "Content"
        };

        mediator
            .Setup(m => m.Send(It.IsAny<ExportPdfCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PdfDTO { PdfBytes = null, FileName = "Report.pdf" });

        var sut = CreateSut(mediator, CreateUser(Guid.NewGuid()));

        //Act
        var result = await sut.ExportPdf(command);

        //Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Report.pdf", notFound.Value);

        mediator.Verify(m => m.Send(It.IsAny<ExportPdfCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMyPermissions_WhenCalled_ReturnsOkObjectResult()
    {
        //Arrange
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        var userId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        var dto = new DocumentPermissionsDTO { IsOwner = true, CanEdit = true };

        mediator
            .Setup(m => m.Send(It.Is<GetPermissionsByUserIdQuery>(q => q.DocumentId == documentId && q.UserId == userId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var sut = CreateSut(mediator, CreateUser(userId));

        //Act
        var result = await sut.GetMyPermissions(documentId);

        //Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(dto, ok.Value);

        mediator.Verify(m => m.Send(It.Is<GetPermissionsByUserIdQuery>(q => q.DocumentId == documentId && q.UserId == userId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetPermission_WhenCalled_SendsUpdatePermissionCommand_AndReturnsNoContent()
    {
        //Arrange
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        var ownerId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        mediator
            .Setup(m => m.Send(It.IsAny<UpdatePermissionCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut(mediator, CreateUser(ownerId));

        //Act
        var result = await sut.SetPermission(documentId, new UpdatePermissionDTO { UserId = targetUserId, CanEdit = true });

        //Assert
        Assert.IsType<NoContentResult>(result);

        mediator.Verify(m => m.Send(It.Is<UpdatePermissionCommand>(c =>
            c.OwnerId == ownerId &&
            c.DocumentId == documentId &&
            c.UserId == targetUserId &&
            c.CanEdit
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static DocumentsController CreateSut(Mock<IMediator> mediator, ClaimsPrincipal? user = null)
    {
        var sut = new DocumentsController(mediator.Object);

        sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = user ?? new ClaimsPrincipal(new ClaimsIdentity())
            }
        };

        return sut;
    }

    private static ClaimsPrincipal CreateUser(Guid userId)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "test"));
    }
}
