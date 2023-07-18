using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiegoG.TelegramBot.Types;
using DiegoG.TelegramBot;
using DiegoG.Utilities.Settings;
using Telegram.Bot.Types;
using System.Diagnostics;
using System.IO;
using File = System.IO.File;

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
                    await Task.WhenAll(new[] { Settings<WatcherSettings>.SaveSettingsAsync(), Task.Delay(1000) });
                    var proc = Process.GetCurrentProcess();
                    var startinfo = new ProcessStartInfo();

                    foreach (var arg in Environment.GetCommandLineArgs())
                        startinfo.ArgumentList.Add(arg);

                    var launch = Environment.GetCommandLineArgs()[0];
                    string exe;

                    if (launch.EndsWith(".dll"))
                    {
                        var potential_exe = launch.Replace(".dll", ".exe");
                        exe = File.Exists(potential_exe)
                            ? potential_exe
                            : throw new InvalidOperationException("Couldn't find an executable file to launch the app");
                    }
                    else
                        exe = launch;

                    Process.Start(new ProcessStartInfo(exe, Environment.CommandLine.Replace(launch, ""))
                    {
                        WorkingDirectory = Directory.GetCurrentDirectory()
                    });
                    Environment.Exit(0);
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
            return new(args.Message, false, "You do not have permissions to perform this operation");

        Held.Add(args.User);
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMinutes(1));
            Cancel(args.User);
        });
        return new(args.Message, true, "Are you sure you want the bot to restart? Please write 'yes' if so.");
    }
}
