using DiegoG.TelegramBot.Types;
using DiegoG.Utilities.Collections;
using DiegoG.Utilities.Reflection;
using DiegoG.Utilities.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace DiegoG.WebWatcher.BotCommands
{
    [BotCommand]
    public class ExtensionsCommand : IBotCommand
    {
        private readonly static Version ExpectedVersion = new(0, 8, 0);

        public string HelpExplanation => "Provides access to extension loading";

        public string HelpUsage => "/extensions";

        public IEnumerable<(string Option, string Explanation)>? HelpOptions { get; } = new[]
        {
            ("help","When used as a reply to an /extensions call, provides this message"),
            ("list loaded","Lists all loaded extensions available"),
            ("list unloaded","Lists all unloaded extensions available"),
            ("list enabled","Lists all enabled extensions"),
            ("list disabled", "Lists all disabled extensions"),
            ("load [file]", "Loads the specified file; write 'all' to load all unlodaded extensions available"),
            ("done","Allows you to issue other commands"),
            ("enable", "Enables the specified extensions, but does not load them"),
            ("disable", "Disables the specified extensions, requires a restart")
        };

        public string Trigger => "/extensions";

        public string? Alias => "/ext";

        public Task<(string Result, bool Hold)> Action(BotCommandArguments args)
        => args.Arguments.Length == 1
                ? OutputBot.GetAdmin(args.User.Id, out var admin) && admin.Rights >= OutputBot.AdminRights.Moderator
                    ? Task.FromResult(("What do you want to do? Hint: write 'help' or 'done'", true)) //It's not necessary to hold user state, since enough state data is stored to bring them down one level, which is enough
                    : Task.FromResult(("You do not have the rights to do that", false))
                : Task.FromResult(("Too many arguments. Please write /extensions", false));

        public Task<(string Result, bool Hold)> ActionReply(BotCommandArguments args)
        {
            var arg = args.Arguments[0].ToLower();
            if (arg == "done")
                return Task.FromResult(("Finished.", false));

            return Task.FromResult((arg switch
            {
                "help" => Help(),
                "list" => List(),
                "load" => Load(),
                "enable" => Enable(),
                "disable" => Disable(),
                _ => "Unknown action"
            }, true));

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
                    "enabled" => Enumerate(Settings<WatcherSettings>.Current.ExtensionsEnable.Where(s => s.Value).Select(d => d.Key)),
                    "disabled" => Enumerate(Settings<WatcherSettings>.Current.ExtensionsEnable.Where(s => !s.Value).Select(d => d.Key)),
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
                foreach (var ext in args.Arguments.StartingAtIndex(0))
                    list.Add(ExtensionLoader.ExtensionDirectory + ext);
                Program.LoadExtensions(list.ToArray());
                return "Loaded enabled extensions";
            }

            string Enable()
            {
                var settings = Settings<WatcherSettings>.Current.ExtensionsEnable;
                foreach (var ext in args.Arguments.StartingAtIndex(0))
                {
                    if (!settings.ContainsKey(ext))
                        settings.Add(ext, true);
                    settings[ext] = true;
                }
                return "Enabled the extensions, be sure to load them";
            }

            string Disable()
            {
                var settings = Settings<WatcherSettings>.Current.ExtensionsEnable;
                foreach (var ext in args.Arguments.StartingAtIndex(0))
                {
                    if (!settings.ContainsKey(ext))
                        settings.Add(ext, false);
                    settings[ext] = false;
                }
                return "Disabled the extensions. Requires a Restart";
            }
        }

        public void Cancel(User user) { }
        bool IBotCommand.Validate([NotNullWhen(false)]out string? message)
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
}
