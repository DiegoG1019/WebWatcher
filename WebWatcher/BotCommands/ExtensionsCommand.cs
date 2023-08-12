using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DiegoG.TelegramBot;
using DiegoG.TelegramBot.Types;
using DiegoG.Utilities.Reflection;
using Telegram.Bot.Types;

namespace DiegoG.WebWatcher.BotCommands;

[BotCommand]
public class ExtensionsCommand : IBotCommand
{
    private static readonly Version ExpectedVersion = new(0, 8, 0);

    public string HelpExplanation => "Provides access to extension loading";

    public string HelpUsage => "/extensions";

    public IEnumerable<OptionDescription>? HelpOptions { get; } = new OptionDescription[]
    {
        new("help","When used as a reply to an /extensions call, provides this message"),
        new("list loaded","Lists all loaded extensions available"),
        new("list unloaded","Lists all unloaded extensions available"),
        new("list enabled","Lists all enabled extensions"),
        new("list disabled", "Lists all disabled extensions"),
        new("load [file]", "Loads the specified file; write 'all' to load all unlodaded extensions available"),
        new("done","Allows you to issue other commands"),
        new("enable", "Enables the specified extensions, but does not load them"),
        new("disable", "Disables the specified extensions, requires a restart")
    };

    public string Trigger => "/extensions";

    public string? Alias => "/ext";

    public TelegramBotCommandClient Processor { get; set; }

    public async Task<CommandResponse> Action(BotCommandArguments args)
    => args.Arguments.Length == 1
            ? OutputBot.GetAdmin(args.User.Id, out var admin) && admin.Rights >= AdminRights.Moderator
                ? new(args.Message, true, "What do you want to do? Hint: write 'help' or 'done'") //It's not necessary to hold user state, since enough state data is stored to bring them down one level, which is enough
                : new(args.Message, false, "You do not have the rights to do that")
            : new(args.Message, false, "Too many arguments. Please write /extensions");

    public async Task<CommandResponse> ActionReply(BotCommandArguments args)
    {
        var arg = args.Arguments[0].ToLower();

        return arg == "done"
            ? (new(args.Message, false, "Finished."))
            : (new(args.Message, true, arg switch
            {
                "help" => Help(),
                "list" => List(),
                "load" => Load(),
                "enable" => Enable(),
                "disable" => Disable(),
                _ => "Unknown action"
            }));

        string Help()
        {
            string s = "";
            foreach (var (o, e) in HelpOptions!)
                s += $"{o}: {e}\n";
            return s;
        }

        string List()
        {
            if (args.Arguments.Length < 2)
                return "Please specify what you want to list";
            return args.Arguments[1].ToLower() switch
            {
                "loaded" => Enumerate(ExtensionLoader.LoadedExtensions.Select(s => Path.GetFileName(s))),
                "unloaded" => Enumerate(ExtensionLoader.EnumerateUnloadedAssemblies().Select(s => Path.GetFileName(s))),
                "enabled" => Enumerate(WatcherSettings.Current.ExtensionsEnable.Where(s => s.Value).Select(d => d.Key)),
                "disabled" => Enumerate(WatcherSettings.Current.ExtensionsEnable.Where(s => !s.Value).Select(d => d.Key)),
                _ => "Unknown action"
            };

            string Enumerate(IEnumerable<string> enumeration)
            {
                var s = "";
                foreach (var n in enumeration)
                    s += $"-> {n}\n";
                return string.IsNullOrWhiteSpace(s) ? "Nothing to show" : s;
            }
        }

        string Load()
        {
            var arg = args.Arguments[1];
            if (arg == "all")
            {
                Program.LoadExtensions(ExtensionLoader.EnumerateUnloadedAssemblies().ToArray());
                return "Loaded all enabled extensions";
            }
            var list = new List<string>(args.Arguments.Length);
            foreach (var ext in args.Arguments.Skip(1))
                list.Add(ExtensionLoader.ExtensionDirectory + ext);
            Program.LoadExtensions(list.ToArray());
            return "Loaded enabled extensions";
        }

        string Enable()
        {
            var settings = WatcherSettings.Current.ExtensionsEnable;
            foreach (var ext in args.Arguments.Skip(1))
            {
                if (!settings.ContainsKey(ext))
                    settings.Add(ext, true);
                settings[ext] = true;
            }
            return "Enabled the extensions, be sure to load them";
        }

        string Disable()
        {
            var settings = WatcherSettings.Current.ExtensionsEnable;
            foreach (var ext in args.Arguments.Skip(1))
            {
                if (!settings.ContainsKey(ext))
                    settings.Add(ext, false);
                settings[ext] = false;
            }
            return "Disabled the extensions. Requires a Restart";
        }
    }

    public void Cancel(User user) { }
    bool IBotCommand.Validate([NotNullWhen(false)] out string? message)
    {
        if (Program.Version < ExpectedVersion)
        {
            message = $"Host is out of date. Expected version v.{ExpectedVersion}";
            return false;
        }
        message = null;
        return true;
    }
}
