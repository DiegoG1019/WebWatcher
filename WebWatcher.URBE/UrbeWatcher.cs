using DiegoG.Utilities.Settings;
using DiegoG.WebWatcher;

namespace WebWatcher.URBE;

[Watcher]
public class UrbeWatcher : IWebWatcher
{
    public const string UrbeUri = "https://inscripciones.urbe.edu/InscripcionWeb/";
    public static bool IsActive { get; private set; }

    public string Name { get; } = "urbe_watcher";
    public TimeSpan Interval { get; } = TimeSpan.FromSeconds(30);

    private readonly HttpClient Http = new();

    public async Task Check()
    {
        var msg = await Http.GetAsync(UrbeUri);
        if (msg.IsSuccessStatusCode is false)
        {
            if (IsActive)
            {
                OutBot.EnqueueAction(x => x.SendTextMessageAsync(Settings<WatcherSettings>.Current.LogChatId, $"URBE is down again! See at {UrbeUri}", disableWebPagePreview: true));
                IsActive = false;
            }
        }
        else if (IsActive is false)
        {
            OutBot.EnqueueAction(x => x.SendTextMessageAsync(Settings<WatcherSettings>.Current.LogChatId, $"URBE is up! See at {UrbeUri}", disableWebPagePreview: true));
            IsActive = true;
        }
    }

    public Task FirstCheck() => Task.CompletedTask;
}
