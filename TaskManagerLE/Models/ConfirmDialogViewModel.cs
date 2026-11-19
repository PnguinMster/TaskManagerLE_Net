namespace TaskManagerLE.Models;

// Settings for the shared _ConfirmDialog partial (move to trash, delete forever).
// `required` makes the compiler insist every caller sets each one.
public class ConfirmDialogViewModel
{
    // Element ids are "<IdPrefix>-dialog", "-title", "-detail", "-ok", "-cancel" (site.js, setUpConfirmDialog).
    public required string IdPrefix { get; init; }
    public required string Title { get; init; }

    // Shown after the item's name, e.g. "will move to the Trash."
    public required string Message { get; init; }
    public required string ConfirmLabel { get; init; }

    // "btn-soft-danger" for reversible (trash), "btn-danger" for permanent (purge).
    public required string ConfirmButtonClass { get; init; }

    // Partial for the small icon in the summary row: "_FolderIcon" or "_TaskIcon".
    public required string SummaryIconPartial { get; init; }
}
