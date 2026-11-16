using TaskMangerLE.Models.Enums;

namespace TaskMangerLE.Extensions;

// How a project status looks on screen. The classes are in site.css: .badge-*, .progress-bar.is-*, .text-*.
public record ProjectStatusStyle(string Label, string BadgeClass, string ProgressClass, string TextClass);

public static class ProjectStatusExtensions
{
    // The one place that maps a status to its label and colours, so every page shows a status the same way.
    // The progress bar uses the same colour as the badge.
    public static ProjectStatusStyle GetStyle(this ProjectStatus status) => status switch
    {
        ProjectStatus.Active => new("In Progress", "badge-blue", "is-blue", "text-blue"),
        ProjectStatus.Planning => new("Planning", "badge-purple", "is-purple", "text-purple"),
        ProjectStatus.OnHold => new("On Hold", "badge-amber", "is-amber", "text-amber"),
        ProjectStatus.Completed => new("Completed", "badge-green", "is-green", "text-green"),
        ProjectStatus.Cancelled => new("Cancelled", "badge-rose", "is-rose", "text-rose"),
        ProjectStatus.Archived => new("Archived", "badge-muted", "is-muted", "muted"),
        // Enums can hold any int (e.g. a value added later), so the compiler wants a fallback.
        _ => new(status.ToString(), "badge-muted", "is-muted", "muted")
    };
}
