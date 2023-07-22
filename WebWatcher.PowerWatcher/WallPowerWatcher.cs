using DiegoG.WebWatcher;

namespace WebWatcher.PowerWatcher;

public class WallPowerWatcher : IWebWatcher
{
    public string Name => "Wall Power Watcher";
    public TimeSpan Interval => TimeSpan.FromMinutes(5);

    public 

    public Task Check()
    {

    }

    public Task FirstCheck()
    {

    }
}
