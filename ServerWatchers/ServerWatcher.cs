using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiegoG.WebWatcher
{
    [Watcher]
    public class ServerWatcher : IWebWatcher
    {
        public string Name => "ServerWatcher";

        public TimeSpan Interval => TimeSpan.FromDays(1); //No features yet

        public Task Check() => Task.CompletedTask;

        public Task FirstCheck() => Task.CompletedTask;
    }
}