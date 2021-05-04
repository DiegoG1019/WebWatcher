using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiegoG.Utilities.Settings;

namespace DiegoG.WebWatcher
{
    public class WatcherSettings : ISettings
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public string SettingsType => "DiegoG.WebWatcher.Secrets";
        public ulong Version => 0;
        public Serilog.Events.LogEventLevel LogEventLevel { get; set; } = Serilog.Events.LogEventLevel.Information;
        public string? BotAPIKey { get; set; }

        public WatcherSettings()
        {
#if DEBUG
            LogEventLevel = Serilog.Events.LogEventLevel.Verbose;
#endif
        }
    }
}
