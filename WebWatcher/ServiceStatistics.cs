using DiegoG.Utilities.Basic;
using DiegoG.Utilities.IO;
using System.Collections.Generic;

namespace DiegoG.WebWatcher;

public class ServiceStatistics
{
    public ulong TotalTasksAwaited { get; set; }
    public ulong OverworkedLoops { get; set; }

    public AverageList AverageTasksPerLoop { get; } = new(20);
    public AverageList AverageLoopsPerSecond { get; } = new(20);

    public readonly Dictionary<string, ulong> TotalRuns = new();

    public override string ToString()
        => Serialization.Serialize.Json(this);
}
