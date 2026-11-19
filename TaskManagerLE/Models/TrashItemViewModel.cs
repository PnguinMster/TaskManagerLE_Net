namespace TaskManagerLE.Models;

// One row on the Trash page: either a deleted project or a task deleted on its own.
public class TrashItemViewModel
{
    public bool IsProject { get; init; }
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateTime? DeletedAtUtc { get; init; }

    // Projects only: how many tasks went to the Trash with it.
    public int TaskCount { get; init; }

    // Tasks only: the (not deleted) project the task belongs to.
    public int? ProjectId { get; init; }
    public string? ProjectName { get; init; }
}
