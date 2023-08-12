using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DiegoG.TelegramBot;
using DiegoG.TelegramBot.Types;
using DiegoG.Utilities.Reflection;
using Humanizer;
using Telegram.Bot.Types;

namespace DiegoG.WebWatcher.BotCommands;

[BotCommand]
internal class Status : IBotCommand
{
    public string HelpExplanation { get; } = "Retrieves the current Status of the bot";

    public string HelpUsage { get; } = "/status [option]";

    public IEnumerable<OptionDescription>? HelpOptions { get; } = new OptionDescription[]
    {
        new("/status stats","Provides Bot Service Statistics"),
        new("/status admins","Provides a list of admins and their rights"),
        new("/status watchers","Provides a list of Watchers")
    };

    public string Trigger => "/status";

    public string? Alias => null;

    public TelegramBotCommandClient Processor { get; set; }

    public async Task<CommandResponse> Action(BotCommandArguments arguments)
    {
        var user = arguments.User;
        OutputBot.GetAdmin(user.Id, out var admin);

        var args = arguments.Arguments;
        if (args.Length > 2)
            return new CommandResponse(arguments.Message, false, "Too many arguments");

        if (args.Length == 1)
            return new CommandResponse(arguments.Message, false, $"Alive and well. Running Time: {Program.RunningTime.Humanize()}\n\nRunning WebWatcher Version {Program.Version} under Library Version {WatcherData.LibraryVersion}");

        if (args[1] == "stats")
            return await Task.Run(() =>
            {
                var s = WatcherService.Statistics;

                var str = Program.GetSharedStringBuilder().AppendLine("Statistics Report");
                foreach (var (p, v) in ReflectionCollectionMethods<ServiceStatistics>.GetAllInstancePropertyNameValueTuple(s))
                    if (v is not IDictionary)
                        str.Append($"{p ?? "Unknown Property"} : {v}\n");

                str.Append("\nTotal Commands Executed Per User:");

                var isMod = (admin?.Rights ?? 0) >= AdminRights.Moderator;

                foreach (var kv in GlobalStatistics.TotalCommandsExecutedPerUser)
                {
                    str.Append($"\nCommand: {kv.Key}");
                    if (isMod)
                        foreach (var v in kv.Value)
                            str.Append($"\n\tUser {v.Key}: {v.Value}");
                    else
                    {
                        kv.Value.TryGetValue(user.Id, out var val);
                        str.Append($"\n\tUser {user.Id}: {val}");
                    }
                }

                str.Append($"\n\nTotal Watch Routine Runs:");
                foreach (var kv in s.TotalRuns)
                    str.Append($"\n\t{kv.Key}: {kv.Value}");

                str.Append($"\n\nTotal Subscription Report Runs:");
                foreach (var kv in s.TotalRuns)
                    str.Append($"\n\t{kv.Key}: {kv.Value}");

                return new CommandResponse(arguments.Message, false, str.ToString());

            });

        if (args[1] == "admins")
            return await Task.Run(() =>
            {
                var s = Program.GetSharedStringBuilder();
                foreach (var a in OutputBot.AccessList)
                    s.Append($"{a.User} - {a.Rights}\n");
                return new CommandResponse(arguments.Message, false, s.ToString());
            });

        if (args[1] == "watchers")
            return await Task.Run(() =>
            {
                var s = Program.GetSharedStringBuilder().Append("Available Watchers:\n");
                foreach (var w in WatcherService.AvailableWatchers)
                    s.Append($"{w}\n");
                return new CommandResponse(arguments.Message, false, s.ToString());
            });

        return new(arguments.Message, false, "Unknown option");
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
