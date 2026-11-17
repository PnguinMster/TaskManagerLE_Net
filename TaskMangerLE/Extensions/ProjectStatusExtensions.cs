using TaskMangerLE.Models.Enums;

namespace TaskMangerLE.Extensions;

// How a project status looks on screen. The classes are in site.css: .badge-*, .progress-bar.is-*, .text-*.
public record ProjectStatusStyle(string Label, string BadgeClass, string ProgressClass, string TextClass);

public static class ProjectStatusExtensions
{
    // Finished = no more work expected: never overdue, never "due soon", sorted last, and
    // left out of the default ("Open") view. Every page uses this one rule.
    public static bool IsFinished(this ProjectStatus status) =>
        status is ProjectStatus.Completed or ProjectStatus.Cancelled or ProjectStatus.Archived;

    // The one place that maps a status to its colours, so every page shows a status the same way.
    // The progress bar uses the same colour as the badge. The label comes from [Display] on the enum.
    public static ProjectStatusStyle GetStyle(this ProjectStatus status)
    {
        (string badgeClass, string progressClass, string textClass) = status switch
        {
            ProjectStatus.Active => ("badge-blue", "is-blue", "text-blue"),
            ProjectStatus.Planning => ("badge-purple", "is-purple", "text-purple"),
            ProjectStatus.OnHold => ("badge-amber", "is-amber", "text-amber"),
            ProjectStatus.Completed => ("badge-green", "is-green", "text-green"),
            ProjectStatus.Cancelled => ("badge-rose", "is-rose", "text-rose"),
            ProjectStatus.Archived => ("badge-muted", "is-muted", "muted"),
            // Enums can hold any int (e.g. a value added later), so the compiler wants a fallback.
            _ => ("badge-muted", "is-muted", "muted")
        };

        return new ProjectStatusStyle(status.GetDisplayName(), badgeClass, progressClass, textClass);
    }
}
