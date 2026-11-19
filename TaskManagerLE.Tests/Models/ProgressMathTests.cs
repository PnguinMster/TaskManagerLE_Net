using TaskManagerLE.Models;

namespace TaskManagerLE.Tests.Models;

public class ProgressMathTests
{
    [Theory]
    [InlineData(0, 0, 0)]    // no tasks: 0% instead of dividing by zero
    [InlineData(0, 5, 0)]    // tasks, but none done
    [InlineData(1, 8, 13)]   // 12.5: banker's rounding would give 12
    [InlineData(1, 3, 33)]   // 33.33 rounds down
    [InlineData(2, 3, 67)]   // 66.67: rounding gives 67, truncation would give 66
    [InlineData(3, 3, 100)]  // all done
    public void Percent_VariousCounts_ReturnsRoundedPercent(int completed, int total, int expected)
    {
        // Act
        int result = ProgressMath.Percent(completed, total);

        // Assert
        Assert.Equal(expected, result);
    }
}
