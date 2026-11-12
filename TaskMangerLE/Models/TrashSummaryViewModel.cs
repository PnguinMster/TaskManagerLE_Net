namespace TaskMangerLE.Models;

public class TrashSummaryViewModel
{
    public int ProjectCount { get; init; }

    // Includes every task that would be deleted, also those inside deleted projects.
    public int TaskCount { get; init; }

    public int TotalCount => ProjectCount + TaskCount;

    // The table rows, newest first. Tasks inside a deleted project are not listed separately;
    // they are counted on their project's row.
    public IReadOnlyList<TrashItemViewModel> Items { get; init; } = [];
}
