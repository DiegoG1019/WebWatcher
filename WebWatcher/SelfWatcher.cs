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
        public TimeSpan Interval { get; } = TimeSpan.FromMinutes(5);

        public string Name => "StatusWatcher";

        public Task Check()
        {
            return Settings<WatcherSettings>.SaveSettingsAsync();
        }

        public Task FirstCheck()
        {
            OutBot.EnqueueAction(b => b.SetMyCommandsAsync(OutBot.Processor.CommandList.AvailableCommands));
            return Task.CompletedTask;
        }
    }
}
