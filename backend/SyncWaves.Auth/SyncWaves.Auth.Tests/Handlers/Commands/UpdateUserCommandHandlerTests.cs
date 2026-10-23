using Microsoft.AspNetCore.Http;
using Moq;
using SyncWaves.Auth.Application.Commands;
using SyncWaves.Auth.Application.Handlers.Commands;
using SyncWaves.Auth.Domain.Abstractions;
using SyncWaves.Auth.Domain.Entities;
using SyncWaves.Auth.Domain.Exceptions;

namespace SyncWaves.Auth.UnitTests.Handlers.Commands;

public class UpdateUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenFileIsNull_ThrowsArgumentException()
    {
        //Arrange
        var usersRepo = new Mock<IUsersRepository>(MockBehavior.Strict);
        var storage = new Mock<IProfileImageStorage>(MockBehavior.Strict);

        var handler = new UpdateUserCommandHandler(usersRepo.Object, storage.Object);

        //Act
        var act = () => handler.Handle(new UpdateUserCommand { UserId = Guid.NewGuid(), FormFile = null! }, CancellationToken.None);

        //Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(act);
        Assert.Equal("No_File", ex.Message);
    }

    [Fact]
    public async Task Handle_WhenFileIsEmpty_ThrowsArgumentException()
    {
        //Arrange
        var usersRepo = new Mock<IUsersRepository>(MockBehavior.Strict);
        var storage = new Mock<IProfileImageStorage>(MockBehavior.Strict);

        var file = new FakeFormFile(Array.Empty<byte>(), "pic.png", "image/png");

        var handler = new UpdateUserCommandHandler(usersRepo.Object, storage.Object);

        //Act
        var act = () => handler.Handle(new UpdateUserCommand { UserId = Guid.NewGuid(), FormFile = file }, CancellationToken.None);

        //Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(act);
        Assert.Equal("No_File", ex.Message);
    }

    [Fact]
    public async Task Handle_WhenContentTypeIsInvalid_ThrowsArgumentException()
    {
        //Arrange
        var usersRepo = new Mock<IUsersRepository>(MockBehavior.Strict);
        var storage = new Mock<IProfileImageStorage>(MockBehavior.Strict);

        var file = new FakeFormFile(new byte[] { 1 }, "pic.gif", "image/gif");

        var handler = new UpdateUserCommandHandler(usersRepo.Object, storage.Object);

        //Act
        var act = () => handler.Handle(new UpdateUserCommand { UserId = Guid.NewGuid(), FormFile = file }, CancellationToken.None);

        //Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(act);
        Assert.Equal("Invalid_File_Type", ex.Message);
    }

    [Fact]
    public async Task Handle_WhenFileIsTooLarge_ThrowsArgumentException()
    {
        //Arrange
        var usersRepo = new Mock<IUsersRepository>(MockBehavior.Strict);
        var storage = new Mock<IProfileImageStorage>(MockBehavior.Strict);

        var file = new FakeFormFile(new byte[] { 1 }, "pic.png", "image/png", lengthOverride: 5L * 1024 * 1024 + 1);

        var handler = new UpdateUserCommandHandler(usersRepo.Object, storage.Object);

        //Act
        var act = () => handler.Handle(new UpdateUserCommand { UserId = Guid.NewGuid(), FormFile = file }, CancellationToken.None);

        //Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(act);
        Assert.Equal("File_Too_Large", ex.Message);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsUserNotFoundException_AfterUploading()
    {
        //Arrange
        var usersRepo = new Mock<IUsersRepository>(MockBehavior.Strict);
        var storage = new Mock<IProfileImageStorage>(MockBehavior.Strict);

        var userId = Guid.NewGuid();
        var file = new FakeFormFile(new byte[] { 1, 2, 3 }, "pic.png", "image/png");

        storage
            .Setup(s => s.SaveProfileImageAsync(userId, It.IsAny<Stream>(), "pic.png", "image/png", It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://cdn/pic.png");

        usersRepo
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEntity?)null);

        var handler = new UpdateUserCommandHandler(usersRepo.Object, storage.Object);

        //Act
        var ex = await Assert.ThrowsAsync<UserNotFoundException>(() =>
            handler.Handle(new UpdateUserCommand { UserId = userId, FormFile = file }, CancellationToken.None));

        //Assert
        Assert.Equal("User_Not_Found", ex.Message);

        storage.Verify(s => s.SaveProfileImageAsync(userId, It.IsAny<Stream>(), "pic.png", "image/png", It.IsAny<CancellationToken>()), Times.Once);
        usersRepo.Verify(r => r.UpdateAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        usersRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenValid_Uploads_SetsProfileUrl_Persists_AndReturnsDto()
    {
        //Arrange
        var usersRepo = new Mock<IUsersRepository>(MockBehavior.Strict);
        var storage = new Mock<IProfileImageStorage>(MockBehavior.Strict);

        var userId = Guid.NewGuid();
        var user = new UserEntity("andrei", "andrei@ye.com", "hash") { Id = userId };
        var file = new FakeFormFile(new byte[] { 1, 2, 3 }, "pic.webp", "image/webp");

        storage
            .Setup(s => s.SaveProfileImageAsync(userId, It.IsAny<Stream>(), "pic.webp", "image/webp", It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://cdn/pic.webp");

        usersRepo
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        usersRepo
            .Setup(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        usersRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new UpdateUserCommandHandler(usersRepo.Object, storage.Object);

        //Act
        var dto = await handler.Handle(new UpdateUserCommand { UserId = userId, FormFile = file }, CancellationToken.None);

        //Assert
        Assert.Equal(userId, dto.Id);
        Assert.Equal(user.Email, dto.Email);
        Assert.Equal("https://cdn/pic.webp", dto.ProfileImageUrl);
        Assert.Equal("https://cdn/pic.webp", user.ProfilePictureUrl);

        usersRepo.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        usersRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed class FakeFormFile : IFormFile
    {
        private readonly byte[] _bytes;
        private readonly long? _lengthOverride;

        public FakeFormFile(byte[] bytes, string fileName, string contentType, long? lengthOverride = null)
        {
            _bytes = bytes;
            _lengthOverride = lengthOverride;
            FileName = fileName;
            ContentType = contentType;
        }

        public string ContentType { get; }
        public string ContentDisposition { get; set; } = string.Empty;
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public long Length => _lengthOverride ?? _bytes.LongLength;
        public string Name { get; set; } = "file";
        public string FileName { get; }

        public void CopyTo(Stream target) => target.Write(_bytes, 0, _bytes.Length);
        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default) => target.WriteAsync(_bytes, cancellationToken).AsTask();
        public Stream OpenReadStream() => new MemoryStream(_bytes, writable: false);
    }
}
