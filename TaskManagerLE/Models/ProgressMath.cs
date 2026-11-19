namespace TaskManagerLE.Models;

public static class ProgressMath
{
    // The one progress formula: whole percent, halves round up (12.5% -> 13%), and no tasks = 0%
    // instead of dividing by zero. Callers leave cancelled tasks out of both counts.
    public static int Percent(int completed, int total) =>
        total == 0
            ? 0
            : (int)Math.Round(completed * 100.0 / total, MidpointRounding.AwayFromZero);
}
