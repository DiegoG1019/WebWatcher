using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

#if DEBUG

namespace DiegoG.WebWatcher.Subscriptions;

[Subscription]
public class TestSubscription : ISubscription
{
    public string Name => "TestSubscription";
    public string Description => "A simple subscription that fires every now and then for testing purposes";
    public TimeSpan Interval => TimeSpan.FromSeconds(10);

    private readonly ConcurrentDictionary<ChatId, ulong> ReportsPerUser = new();
    private ulong TotalReports;

    public Task Subscribed(ChatId chat)
    {
        ReportsPerUser.TryAdd(chat, 0);
        return Task.CompletedTask;
    }

    public Task Unsubscribed(ChatId chat)
    {
        ReportsPerUser.TryRemove(chat, out _);
        return Task.CompletedTask;
    }

    public Task Report(IEnumerable<ChatId> subscribers)
    {
        TotalReports++;

        foreach (var sub in subscribers)
        {
            var u = ReportsPerUser.AddOrUpdate(sub, 0, (c, u) => u + 1);
            OutBot.EnqueueAction(x => x.SendTextMessageAsync(sub, $"Subscription Message #{TotalReports}. This is the {u}{NumericEnding(u)} message you receive; taking into account that it's reset if you unsubscribe"));
        }

        return Task.CompletedTask;
    }

    private static string NumericEnding(ulong number)
        => number switch
        {
            1 => "st",
            2 => "nd",
            3 => "rd",
            _ => "th"
        };
}

#endif