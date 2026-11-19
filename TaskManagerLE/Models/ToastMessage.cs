using TaskManagerLE.Models.Enums;

namespace TaskManagerLE.Models;

// A toast for the next page the user sees. Stored in TempData so it survives the redirect
// after a POST (see ToastExtensions), then shown by site.js.
// ActionLabel + ActionUrl add a button (e.g. "Undo") that POSTs to ActionUrl when clicked.
public record ToastMessage(
    ToastKind Kind,
    string Title,
    string? Message = null,
    string? ActionLabel = null,
    string? ActionUrl = null);
