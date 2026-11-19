using TaskManagerLE.Models.Enums;

namespace TaskManagerLE.Models;

// What the shared _ProgressBar partial needs, so project cards, list rows and the home page all show
// progress the same way. Status picks the colour; ProjectName goes into the screen reader label.
public record ProgressBarViewModel(
    int CompletedTaskCount,
    int TotalTaskCount,
    int Percent,
    ProjectStatus Status,
    string ProjectName);
