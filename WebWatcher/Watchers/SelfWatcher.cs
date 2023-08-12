using System;
using System.Threading.Tasks;
using Telegram.Bot;

namespace DiegoG.WebWatcher.Watchers;

[Watcher]
public class StatusWatcher : IWebWatcher
{
    public TimeSpan Interval { get; } = TimeSpan.FromMinutes(1);

    public string Name => "StatusWatcher";

    public Task Check()
    {
        OutBot.EnqueueAction(b => b.SetMyCommandsAsync(OutBot.Processor.CommandList.AvailableCommands));
        return WatcherSettings.SaveToFileAsync();
    }

    public Task FirstCheck()
    {
        OutBot.EnqueueAction(b => b.SetMyCommandsAsync(OutBot.Processor.CommandList.AvailableCommands));
        return Task.CompletedTask;
    }
}
