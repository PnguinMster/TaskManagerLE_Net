using TaskMangerLE.Models.Enums;

namespace TaskMangerLE.Models;

public class ProjectCardViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public ProjectStatus Status { get; init; }
    public string Description { get; init; } = string.Empty;
    public int CompletedTaskCount { get; init; }
    public int TotalTaskCount { get; init; }
    public int ProgressPercent { get; init; }
    public DateOnly DueDate { get; init; }
}