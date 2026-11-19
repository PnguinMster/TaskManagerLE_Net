using TaskManagerLE.Extensions;
using TaskManagerLE.Models.Enums;

namespace TaskManagerLE.Tests.Extensions;

public class DueGroupTests
{
    private static readonly DateOnly Today = new(2026, 3, 1);

    [Theory]
    [InlineData(-30, false, DueGroup.Overdue)]   // long overdue
    [InlineData(-1, false, DueGroup.Overdue)]    // last overdue day
    [InlineData(0, false, DueGroup.Today)]       // due today is not overdue yet
    [InlineData(1, false, DueGroup.ThisWeek)]    // first day after today
    [InlineData(7, false, DueGroup.ThisWeek)]    // last day of the week: catches <= changed to <
    [InlineData(8, false, DueGroup.Later)]       // first day past the week
    [InlineData(-1, true, DueGroup.Finished)]    // finished beats overdue
    [InlineData(0, true, DueGroup.Finished)]     // finished beats today
    public void GetDueGroup_VariousDueDates_ReturnsExpectedGroup(int daysFromToday, bool isFinished, DueGroup expected)
    {
        // Arrange
        DateOnly dueDate = Today.AddDays(daysFromToday);

        // Act
        DueGroup result = dueDate.GetDueGroup(Today, isFinished);

        // Assert
        Assert.Equal(expected, result);
    }
}
