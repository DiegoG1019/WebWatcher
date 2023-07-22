using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DiegoG.TelegramBot;
using DiegoG.TelegramBot.Types;
using Serilog;
using Telegram.Bot.Types;

namespace DiegoG.WebWatcher.BotCommands;

[BotCommand]
public class SubscribeCommand : IBotCommand
{
    public async Task<CommandResponse> Action(BotCommandArguments args)
    {
        if (args.Arguments.Length < 1)
            return new CommandResponse(args, false, "No subscription name provided");

        var sb = args.Arguments[1];
        var subscr = SubscriptionService.AvailableSubscriptionInfo.FirstOrDefault(x => x.Name == sb);
        if (subscr is null)
            return new CommandResponse(args, false, "Unknown subscription");

        if (subscr.Subscribers.Contains(args.FromChat) is false)
        {
            subscr.Subscribers.AddLast(args.FromChat);
            SubscriptionService.SaveSubscriberData();
            return new CommandResponse(args, false, $"Succesfully subscribed to '{sb}'");
        }

        return new CommandResponse(args, false, $"Already subscribed to '{sb}'");
    }

    public Task<CommandResponse> ActionReply(BotCommandArguments args)
    {
        throw new System.NotImplementedException();
    }

    public void Cancel(User user)
    {
        throw new System.NotImplementedException();
    }

    public TelegramBotCommandClient Processor { get; set; }
    public string HelpExplanation => "Subscribe to the given subscription. See /subscriptions to see available subscriptions";
    public string HelpUsage => "/subscribe subscription";
    public IEnumerable<OptionDescription>? HelpOptions { get; }
    public string Trigger => "/subscribe";
    public string? Alias => null;
}
