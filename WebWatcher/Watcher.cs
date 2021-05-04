using DiegoG.Utilities;
using DiegoG.Utilities.Basic;
using DiegoG.Utilities.IO;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DiegoG.WebWatcher
{
    public static class Service
    {
        /// <summary>
        /// The maximum amount of tasks per loop
        /// </summary>
        public static uint MaxTasks { get; set; } = 50;

        private class WatcherTimerPair
        {
            public readonly IWebWatcher Watcher;
            public readonly System.Timers.Timer Timer;
#if DEBUG
            private static int i = 0;
            private readonly int id;
#endif
            public WatcherTimerPair(IWebWatcher watcher)
            {
#if DEBUG
                id = i++;
                Log.Verbose($"New WatcherTimerPair created, ID: {id}");
#endif
                Watcher = watcher;
                Timer = new(watcher.Interval.TotalMilliseconds);
                Timer.AutoReset = true;
            }
#if DEBUG
            ~WatcherTimerPair()
            {
                Log.Verbose($"WatcherTimerPair deleted, ID: {id}");
            }
#endif
        }

        private static readonly Queue<Func<Task>> ActionQueue = new();
        private static readonly List<WatcherTimerPair> Watchers = new();
        public static void AddWatcher(IWebWatcher watcher)
        {
            WatcherTimerPair pair = new(watcher);
            Watchers.Add(pair);
            ActionQueue.Enqueue(pair.Watcher.FirstCheck);
            pair.Timer.Elapsed += (s, e) => ActionQueue.Enqueue(pair.Watcher.Check);
            pair.Timer.Start();
        }

        public static void RemoveWatcher(IWebWatcher watcher)
        {
            var i = Watchers.FindIndex(p => p.Watcher.Equals(watcher));
            var w = Watchers[i];
            Watchers.RemoveAt(i);
            w.Timer.Stop();
        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public static StatisticsReport DaemonStatistics { get; internal set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public static async Task Active(CancellationToken token)
        {
            Log.Information($"Started Running WebWatcher v.{Program.Version}");
            AsyncTaskManager tasks = new();
            Log.Information("Starting Daemon");

            DaemonStatistics = new() { StartTime = DateTime.Now };

            Log.Information("Entering Main Loop");
            while (!token.IsCancellationRequested)
            {
                var throttletask = Task.Delay(50, CancellationToken.None);

                for (int i = 0; i < MaxTasks && ActionQueue.Count > 0; i++)
                    tasks.Add(ActionQueue.Dequeue()());

                //--
                DaemonStatistics.TotalTasksAwaited += (ulong)tasks.Count;
                DaemonStatistics.ATasksPL.AddSample(tasks.Count);

                await tasks;
                tasks.Clear();

                if (throttletask.IsCompleted)
                    DaemonStatistics.OverworkedLoops++;
                else
                    await throttletask;
            }
            Log.Information("Termination Signal Received, Stopping Daemon");
        }

        public sealed record StatisticsReport
        {
            public DateTime StartTime { get; internal init; }

            public TimeSpan UpTime => DateTime.Now - StartTime;

            public ulong TotalActionsExecuted { get; internal set; }
            public ulong TotalTasksAwaited { get; internal set; }
            public ulong OverworkedLoops { get; internal set; }

            internal AverageList AActionsPL { get; } = new(20);
            internal AverageList ATasksPL { get; } = new(20);

            public override string ToString()
                => Serialization.Serialize.Json(this);

            internal StatisticsReport() { }
        }
    }
}
