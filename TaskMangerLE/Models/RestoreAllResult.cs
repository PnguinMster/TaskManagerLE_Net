namespace TaskMangerLE.Models;

// How an item was deleted, saved just before Restore all clears it. Undo uses it to put the
// item back in the Trash exactly as it was: same deletion time and user. RestoreProjectAsync
// matches a project's tasks by that deletion time, so a new time would break it.
public record TrashedItemSnapshot(int Id, DateTime? DeletedAtUtc, string? DeletedByUserId);

// What Restore all brought back. Stored in TempData (as JSON) until the toast's Undo is clicked.
public record RestoreAllResult(List<TrashedItemSnapshot> Projects, List<TrashedItemSnapshot> Tasks);
