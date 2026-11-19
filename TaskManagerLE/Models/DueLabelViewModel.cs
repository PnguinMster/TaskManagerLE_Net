namespace TaskManagerLE.Models;

// What the shared _DueLabel partial needs, so projects and tasks can both use it.
public record DueLabelViewModel(DateOnly DueDate, bool IsFinished);
