using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiegoG.Utilities.Settings;

namespace DiegoG.WebWatcher;

public class WatcherSettings : ISettings
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public string SettingsType => "DiegoG.WebWatcher.Secrets";
    public ulong Version => 7;
    public Serilog.Events.LogEventLevel FileLogEventLevel { get; set; } = Serilog.Events.LogEventLevel.Debug;
    public Serilog.Events.LogEventLevel BotLogEventLevel { get; set; } = Serilog.Events.LogEventLevel.Information;
    public Serilog.Events.LogEventLevel ConsoleLogEventLevel { get; set; } = Serilog.Events.LogEventLevel.Debug;
    public string? BotAPIKey { get; set; }

    public string? VersionName { get; set; } = "";

    public long LogChatId { get; set; } = 0;

    public IDictionary<string, bool> SubscriptionEnableList { get; set; } = new Dictionary<string, bool>();
    public IDictionary<string, bool> WatcherEnableList { get; set; } = new Dictionary<string, bool>();

    public IDictionary<string, bool> ExtensionsEnable { get; set; } = new Dictionary<string, bool>();

    public string? RestartCommand { get; set; }
    public string? RestartCommandArguments { get; set; }

    public WatcherSettings()
    {
#if DEBUG
        FileLogEventLevel = Serilog.Events.LogEventLevel.Verbose;
#endif
    }
}
