using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using DiegoG.TelegramBot;
using DiegoG.TelegramBot.Types;
using Telegram.Bot.Types;

namespace DiegoG.WebWatcher.BotCommands;

[BotCommand]
public class RestartCommand : IBotCommand
{
    public string HelpExplanation { get; } = "Requests the bot server to restart the bot";

    public string HelpUsage { get; } = "/restart";

    public IEnumerable<OptionDescription>? HelpOptions => null;

    public string Trigger => "/restart";

    public string? Alias => null;

    public TelegramBotCommandClient Processor { get; set; }

    private List<User> Held { get; } = new();
    public IEnumerable<User>? Hold => Held;

    public void Cancel(User user)
    {
        if (Held.Contains(user))
            Held.Remove(user);
    }

    public async Task<CommandResponse> ActionReply(BotCommandArguments args)
    {
        if (Held.Contains(args.User))
        {
            if (args.Arguments[0] == "yes")
            {
                _ = Task.Run(async () =>
                {
                    await Task.WhenAll(new[] { WatcherSettings.SaveToFileAsync(), Task.Delay(1000) });

                    if (WatcherSettings.Current.RestartCommand is string cmd)
                        Process.Start(new ProcessStartInfo(cmd)
                        {
                            Arguments = WatcherSettings.Current.RestartCommandArguments,
                            UseShellExecute = true
                        });
                    else
                        throw new InvalidOperationException("The RestartCommand was lost somehow. The RestartCommand property of settings should not be modified after startup");
                });
                Cancel(args.User);
                return new(args.Message, false, "Bot restarting down in 1000ms");
            }
            Cancel(args.User);
            return new(args.Message, false, "Cancelling Restart order");
        }
        return new(args.Message, false, "Restart order automatically canceled");
    }

    public async Task<CommandResponse> Action(BotCommandArguments args)
    {
        if (!OutputBot.GetAdmin(args.User.Id, out var adm) || adm.Rights < AdminRights.Admin)
            return new(args, false, "You do not have permissions to perform this operation");

        if (WatcherSettings.Current.RestartCommand is not string cmd)
            return new(args, false, "This instance of WebWatcher was not configured with a RestartCommand. Check the Settings file and restart manually to configure it.");

        Held.Add(args.User);
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMinutes(1));
            Cancel(args.User);
        });
        return new(args.Message, true, "Are you sure you want the bot to restart? Please write 'yes' if so.");
    }
}
