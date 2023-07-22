using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace DiegoG.WebWatcher;

public interface ISubscription
{
    public string Name { get; }
    public string Description { get; }
    public TimeSpan Interval { get; }
    public Task Subscribed(ChatId chat);
    public Task Unsubscribed(ChatId chat);
    public Task Report(IEnumerable<ChatId> subscribers);

    /// <summary>
    /// Validates that the current instance of WebWatcher is valid. Can be used to check for version
    /// </summary>
    /// <returns></returns>
    public bool Validate([NotNullWhen(false)] out string? failuremessage)
    {
        failuremessage = null;
        return true;
    }
}
