using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiegoG.WebWatcher
{
    public class StatusWatcher : IWebWatcher
    {
        public TimeSpan Interval { get; } = TimeSpan.FromHours(2);

        public Task Check()
        {
            Log.Debug($"Current Statistics: {Service.DaemonStatistics}");
            return Task.CompletedTask;
        }

        public Task FirstCheck() => Task.CompletedTask;

    }

    public class SelfWatcher : IWebWatcher
    {
        public TimeSpan Interval { get; } = TimeSpan.FromMinutes(15);

        public Task Check()
        {
            Log.Debug($"Alive and well. Running Time {Program.RunningTime}");
            return Task.CompletedTask;
        }

        public Task FirstCheck() => Task.CompletedTask;
    }
}
