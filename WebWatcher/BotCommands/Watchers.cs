using DiegoG.TelegramBot;
using DiegoG.TelegramBot.Types;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace DiegoG.WebWatcher.BotCommands;
[BotCommand]
public class Watchers : IBotCommand
{
    public TelegramBotCommandClient Processor { get; set; }

    public string HelpExplanation => "Interface with Watchers";

    public string HelpUsage => "/watcher (watchername)";

    public IEnumerable<OptionDescription>? HelpOptions { get; } = new OptionDescription[]
    {
        new("(watchername)", "The name of the watcher whose status you wanna see"),
        new("(watchername) (enabled true/false)", "The current status of the watcher. Won't run if disabled")
    };

    public string Trigger => "/watcher";

    public string? Alias => null;

    public async Task<CommandResponse> Action(BotCommandArguments args)
    {
        var a = args.Arguments;
        return a.Length switch
        {
            1 => new(args, false, string.Join("\n>> ", WatcherService.AvailableWatchers)),
            2 => oneSubscription(),
            > 2 => act()
        };

        CommandResponse oneSubscription()
        {
            var builder = Program.GetSharedStringBuilder();

            string r;
            var watcher = WatcherService.AvailableWatcherPairs.FirstOrDefault(x => x.Name == a[1]);

            if (watcher is null)
                r = "Unknown Subscription";
            else
            {
                builder.Append(watcher.Name);

                if ((!OutputBot.GetAdmin(args.User.Id, out var adm) || adm.Rights < AdminRights.Moderator))
                    builder.Append(watcher.Pause ? " [Enabled]" : " [Disabled]");

                r = builder.ToString();
            }

            return new(args, false, r);
        }

        CommandResponse act()
        {
            if (!OutputBot.GetAdmin(args.User.Id, out var adm) || adm.Rights < AdminRights.Moderator)
                return new(args.Message, false, "You do not have permissions to enable or disable watchers");

            var p = WatcherService.AvailableWatcherPairs.FirstOrDefault(x => x.Name == a[1]);
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
