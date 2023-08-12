using System.Collections.Generic;
using System.Linq;

namespace DiegoG.WebWatcher;

public class ServiceStatistics
{
    public ulong TotalTasksAwaited { get; set; }
    public ulong OverworkedLoops { get; set; }

    public CircularList<double> TasksPerLoop { get; } = new(20);
    public CircularList<double> LoopsPerSecond { get; } = new(20);

    public double AverageTasksPerLoop => TasksPerLoop.Average();
    public double AverageLoopsPerSecond => TasksPerLoop.Average();

    public readonly Dictionary<string, ulong> TotalRuns = new();
}
