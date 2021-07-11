using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiegoG.TelegramBot;
using DiegoG.TelegramBot.Types;
using DiegoG.Utilities.Collections;
using DiegoG.Utilities.IO;
using DiegoG.Utilities.Reflection;
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
            ("/status admins","Provides a list of admins and their rights"),
            ("/status watchers","Provides a list of Watchers")
        };

        public string Trigger => "/status";

        public string? Alias => null;

        public BotCommandProcessor Processor { get; set; }

        public Task<(string, bool)> Action(BotCommandArguments arguments)
        {
            var user = arguments.User;
            OutputBot.GetAdmin(user.Id, out var admin);

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
                        if(v is not IDictionary)
                            str += $"{p ?? "Unknown Property"} : {v}\n";

                    str += "\nTotal Commands Executed Per User:";

                    var isMod = (admin?.Rights ?? 0) >= OutputBot.AdminRights.Moderator;
                    
                    foreach (var kv in s.TotalCommandsExecutedPerUser)
                    {
                        str += $"\nCommand: {kv.Key}";
                        if (isMod)
                            foreach (var v in kv.Value)
                                str += $"\n\tUser {v.Key}: {v.Value}";
                        else
                        {
                            kv.Value.TryGetValue(user.Id, out var val);
                            str += $"\n\tUser {user.Id}: {val}";
                        }    
                    }

                    str += $"\n\nTotal Watch Routine Runs:";
                    foreach (var kv in s.TotalWatchRuns)
                        str += $"\n\t{kv.Key}: {kv.Value}";

                    return (str, false);

                });

            if (args[1] == "admins")
                return Task.Run(() =>
                {
                    var s = "";
                    foreach (var a in OutputBot.AccessList)
                        s += $"{a.User} - {a.Rights}\n";
                    return (s, false);
                });

            if (args[1] == "watchers")
                return Task.Run(() =>
                {
                    var s = "Available Watchers:\n";
                    foreach (var w in Service.AvailableWatchers)
                        s += $"{w}\n";
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
