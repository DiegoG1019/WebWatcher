using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DiegoG.TelegramBot;
using DiegoG.TelegramBot.Types;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DiegoG.WebWatcher.BotCommands;

[BotCommand]
public class Subscriptions : IBotCommand
{
    public TelegramBotCommandClient Processor { get; set; }

    public string HelpExplanation => "Query information about available subscriptions. See /unsubscribe and /subscribe to unsubscribe or subscribe respectively to a subscription";

    public string HelpUsage => "/subscription (subscriptionname)";

    public IEnumerable<OptionDescription>? HelpOptions { get; } = new OptionDescription[]
    {
        new("(subscriptionname)", "The name of the subscription whose status you wanna see"),
        new("(subscriptionname) (enabled true/false)", "The current status of the subscription. Won't run if disabled. Moderator rights required.")
    };

    public string Trigger => "/subscription";

    public string? Alias => null;

    public async Task<CommandResponse> Action(BotCommandArguments args)
    {
        var a = args.Arguments;

        return a.Length switch
        {
            1 => allSubscriptions(),
            2 => oneSubscription(),
            > 2 => enableOrDisable()
        };

        CommandResponse oneSubscription()
        {
            var builder = Program.GetSharedStringBuilder();

            string r;
            var subscr = SubscriptionService.AvailableSubscriptionInfo.FirstOrDefault(x => x.Name == a[1]);

            if (subscr is null)
                r = "Unknown Subscription";
            else
            {
                builder.Append(subscr.Name);

                if (subscr.SubscriberList.Contains(args.FromChat))
                    builder.Append(" (Subscribed)");

                if ((!OutputBot.GetAdmin(args.User.Id, out var adm) || adm.Rights < AdminRights.Moderator))
                    builder.Append(subscr.Pause ? " [Enabled]" : " [Disabled]");

                builder.Append("\n > ").Append(subscr.Subscription.Description);

                r = builder.ToString();
            }

            return new(args, false, r);
        }

        CommandResponse allSubscriptions()
        {
            var builder = Program.GetSharedStringBuilder();
            foreach (var subscr in SubscriptionService.AvailableSubscriptionInfo)
            {
                builder.Append("\n - <b>").Append(subscr.Name);
                if (subscr.SubscriberList.Contains(args.FromChat))
                    builder.Append(" (Subscribed)");
                builder.Append("</b>: ").Append(subscr.Subscription.Description);
            }

            var str = builder.ToString();
            return new(false, x => x.SendTextMessageAsync(args.FromChat, str, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html));
        }

        CommandResponse enableOrDisable()
        {
            if ((!OutputBot.GetAdmin(args.User, out var adm) || adm.Rights < AdminRights.Moderator))
                return new(args, false, "You do not have permissions to enable or disable subscriptions");

            var p = SubscriptionService.AvailableSubscriptionInfo.FirstOrDefault(x => x.Name == a[1]);
            if (bool.TryParse(a[2], out var res) && p is not null)
            {
                res = !res;
                p.Pause = res;
                return new(args, false, $"{(res ? "Disabled" : "Enabled")} {p.Name}");
            }
            return new(args, false, $"Invalid argument {a[2]}");
        }
    }

    public Task<CommandResponse> ActionReply(BotCommandArguments args)
    {
        throw new NotImplementedException();
    }

    public void Cancel(User user)
    {
        throw new NotImplementedException();
    }
}
