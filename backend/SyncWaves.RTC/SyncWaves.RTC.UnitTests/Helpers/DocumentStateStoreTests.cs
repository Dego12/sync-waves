using SyncWaves.RTC.Api.Helpers;

namespace SyncWaves.RTC.UnitTests.Helpers;

public class DocumentStateStoreTests
{
    [Fact]
    public async Task GetContent_WhenMissing_ReturnsNull()
    {
        //Arrange
        var store = new DocumentStateStore();
        var docId = Guid.NewGuid().ToString();

        //Act
        var result = await store.GetContentAsync(docId);

        //Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateContent_ThenGetContent_ReturnsUpdated()
    {
        //Arrange
        var store = new DocumentStateStore();
        var docId = Guid.NewGuid().ToString();

        //Act
        await store.UpdateContentAsync(docId, "c");
        var result = await store.GetContentAsync(docId);

        //Assert
        Assert.Equal("c", result);
    }

    [Fact]
    public async Task GetTitle_WhenMissing_ReturnsNull()
    {
        //Arrange
        var store = new DocumentStateStore();
        var docId = Guid.NewGuid().ToString();

        //Act
        var result = await store.GetTitleAsync(docId);

        //Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateTitle_ThenGetTitle_ReturnsUpdated()
    {
        //Arrange
        var store = new DocumentStateStore();
        var docId = Guid.NewGuid().ToString();

        //Act
        await store.UpdateTitleAsync(docId, "t");
        var result = await store.GetTitleAsync(docId);

        //Assert
        Assert.Equal("t", result);
    }
}
