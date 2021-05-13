using DiegoG.Utilities.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiegoG.WebWatcher
{
    public class EraiRawsWatcherSettings : ISettings
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public string SettingsType => "Erai-raws Watcher Settings";
        public ulong Version => 0;

        /// <summary>
        /// A string to format. Must contain "{magnetlink}", and "{directoryname}"
        /// </summary>
        public string StartTorrentCommand { get; set; } = "transmission-cli -ep -w {directoryname} {magnetlink}";

        /// <summary>
        /// To be replaced in StartTorrentCommand as {directoryname}
        /// </summary>
        public string TorrentDirectory { get; set; } = "~/autotorrents";
    }
}