using DiegoG.TelegramBot;
using DiegoG.TelegramBot.Types;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace DiegoG.WebWatcher.BotCommands
{
    [BotCommand]
    public class Watchers : IBotCommand
    {
        public TelegramBotCommandClient Processor { get; set; }

        public string HelpExplanation => "Interface data with Watchers";

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
                1 => new(args, false, string.Join("\n>> ", Service.AvailableWatchers)),
                2 => new(args, false, Service.AvailableWatchers.Any(x => x == a[1]) ? $"Watcher: \"{a[1]}\" is {(Service.AvailableWatcherPairs.First(x => x.Name == a[1]).Pause ? "disabled" : "enabled")}" : "Unknown Watcher"),
                > 2 => act()
            };

            CommandResponse act()
            {
                var p = Service.AvailableWatcherPairs.FirstOrDefault(x => x.Name == a[1]);
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
}
