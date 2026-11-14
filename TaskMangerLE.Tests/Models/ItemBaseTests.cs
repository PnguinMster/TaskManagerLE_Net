using TaskMangerLE.Models;

namespace TaskMangerLE.Tests.Models;

// Unit tests for the soft-delete logic on ItemBase.
// ItemBase is abstract, so we test it through TaskItem, a real subclass.
public class ItemBaseTests
{
    // A fixed time, so the test never depends on the real clock.
    private static readonly DateTime DeletedAt = new(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void NewItem_IsNotDeleted()
    {
        // Arrange + Act: just creating the item is the action here.
        var task = new TaskItem();

        // Assert
        Assert.False(task.IsDeleted);
        Assert.Null(task.DeletedAtUtc);
        Assert.Null(task.DeletedByUserId);
    }

    [Fact]
    public void SoftDelete_SetsDeletedFlagAndAuditFields()
    {
        // Arrange
        var task = new TaskItem();

        // Act
        task.SoftDelete("user-1", DeletedAt);

        // Assert
        Assert.True(task.IsDeleted);
        Assert.Equal("user-1", task.DeletedByUserId);
        Assert.Equal(DeletedAt, task.DeletedAtUtc);
    }

    [Fact]
    public void Restore_AfterSoftDelete_ClearsDeletedFlagAndAuditFields()
    {
        // Arrange: start from a deleted item.
        var task = new TaskItem();
        task.SoftDelete("user-1", DeletedAt);

        // Act
        task.Restore();

        // Assert: back to the same state as a brand-new item.
        Assert.False(task.IsDeleted);
        Assert.Null(task.DeletedByUserId);
        Assert.Null(task.DeletedAtUtc);
    }
}
