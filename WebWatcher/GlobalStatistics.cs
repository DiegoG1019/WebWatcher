using System;
using System.Collections.Generic;

namespace DiegoG.WebWatcher;

public static class GlobalStatistics
{
    public static DateTime StartTime { get; } = DateTime.Now;

    public static TimeSpan UpTime => DateTime.Now - StartTime;

    public static Dictionary<string, Dictionary<long, ulong>> TotalCommandsExecutedPerUser { get; } = new();
}
