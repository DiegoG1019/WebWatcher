using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiegoG.TelegramBot.Types;
using DiegoG.Utilities.Collections;
using DiegoG.Utilities.IO;
using Telegram.Bot.Types;

namespace DiegoG.WebWatcher.BotCommands
{
    [BotCommand]
    class Status : IBotCommand
    {
        public string HelpExplanation { get; } = "Retrieves the current Status of the bot";

        public string HelpUsage { get; } = "/status [option]";

        public IEnumerable<(string Option, string Explanation)>? HelpOptions { get; } = new[]
        {
            ("/status stats","Provides Bot Service Statistics"),
            ("/status admins","Provides a list of admins and their rights")
        };

        public string Trigger => "/status";

        public string? Alias => null;

        public Task<(string, bool)> Action(BotCommandArguments arguments)
        {
            var args = arguments.Arguments;
            if (args.Length > 2)
                return Task.FromResult(("Too many arguments", false));


            if(args.Length == 1)
                return Task.FromResult(($"Alive and well. Running Time: {Program.RunningTime}", false));

            if (args[1] == "stats")
                return Task.Run(() =>
                {
                    var s = Service.DaemonStatistics;
                    var str = "Statistics Report\n";
                    foreach(var (p,v) in ReflectionCollectionMethods<Service.StatisticsReport>.GetAllInstancePropertyNameValueTuple(s))
                        str += $"{p ?? "Unknown Property"} : {v}\n";
                    return (str[..^1], false);
                });

            if (args[1] == "admins")
                return Task.Run(() =>
                {
                    var s = "";
                    foreach (var a in OutputBot.AccessList)
                        s += $"{a.User} - {a.Rights}\n";
                    return (s, false);
                });

            return Task.FromResult(("Unknown option", false));
        }

        public Task<(string Result, bool Hold)> ActionReply(BotCommandArguments args)
        {
            throw new NotImplementedException();
        }

        public void Cancel(User user)
        {
            throw new NotImplementedException();
        }
    }
}
