using DiegoG.WebWatcher;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace WebWatcher.URBE;

[Subscription]
public class UrbeSubscription : ISubscription
{
    public string Name => "urbe_inscripciones";
    public string Description => "Monitors URBE's student sign up page";
    public TimeSpan Interval { get; } = TimeSpan.FromSeconds(30);

    public Task Subscribed(ChatId chat) => Task.CompletedTask;

    public Task Unsubscribed(ChatId chat) => Task.CompletedTask;

    public static bool IsActive { get; private set; }

    private readonly HttpClient Http = new();

    public async Task Report(IEnumerable<ChatId> subscribers)
    {
        const string fail = $"URBE is down again! See at {UrbeWatcher.UrbeUri}";
        const string succ = $"URBE is up! See at {UrbeWatcher.UrbeUri}";

        var msg = await Http.GetAsync(UrbeWatcher.UrbeUri);
        if (msg.IsSuccessStatusCode is false)
        {
            if (IsActive)
            {
                foreach (var subsc in subscribers)
                    OutBot.EnqueueAction(x => x.SendTextMessageAsync(subsc, fail, disableWebPagePreview: true));
                IsActive = false;
            }
        }
        else if (IsActive is false)
        {
            foreach (var subsc in subscribers)
                OutBot.EnqueueAction(x => x.SendTextMessageAsync(subsc, succ, disableWebPagePreview: true));
            IsActive = true;
        }
    }
}
