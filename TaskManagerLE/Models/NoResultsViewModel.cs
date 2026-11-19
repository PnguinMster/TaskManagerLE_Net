namespace TaskManagerLE.Models;

// Settings for the shared _NoResults partial (the "nothing matches your filters" box).
public class NoResultsViewModel
{
    // site.js shows/hides the box by this id (e.g. "task-no-results").
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }

    // The data attribute site.js listens for on the Clear filters button (e.g. "data-task-clear-filters").
    public required string ClearButtonAttribute { get; init; }
}
