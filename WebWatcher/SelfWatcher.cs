using DiegoG.Utilities.Settings;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiegoG.WebWatcher
{
    [Watcher]
    public class StatusWatcher : IWebWatcher
    {
        public TimeSpan Interval { get; } = TimeSpan.FromMinutes(15);

        public string Name => "StatusWatcher";

        public Task Check()
        {
#if DEBUG
            Log.Debug($"Alive and well. Running Time {Program.RunningTime}");
#else
            Log.Verbose($"Alive and well. Running Time {Program.RunningTime}");
#endif

            return Settings<WatcherSettings>.SaveSettingsAsync();
        }

        public Task FirstCheck() => Task.CompletedTask;
    }
}
