using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DiegoG.Utilities.Reflection;
using Serilog;

namespace DiegoG.WebWatcher;

public static class WatcherService
{
    /// <summary>
    /// The maximum amount of tasks per loop
    /// </summary>
    public static uint MaxTasks { get; set; } = 5;

    public class WatcherTimerPair
    {
        public readonly IWebWatcher Watcher;
        public readonly Task Timer;
        public readonly string Name;
        public readonly Action<WatcherTimerPair> Action;
        public bool Pause { get; set; }

        private bool Cancel = false;

        public void Stop()
            => Cancel = true;

#if DEBUG
        private static int i = 0;
        private readonly int id;
#endif
        public WatcherTimerPair(IWebWatcher watcher, Action<WatcherTimerPair> action)
        {
#if DEBUG
            id = i++;
            Log.Verbose($"New WatcherTimerPair created, ID: {id}");
#endif
            Watcher = watcher;
            Action = action;
            Name = watcher.Name;

            Timer = new Task(
                async () =>
                {
                    while (!Cancel)
                    {
                        await Task.Delay(watcher.Interval);
                        if (!Pause)
                            Action(this);
                    }
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning
            );
        }
#if DEBUG
        ~WatcherTimerPair()
        {
            Log.Verbose($"WatcherTimerPair deleted, ID: {id}");
        }
#endif
    }

    public static IEnumerable<string> AvailableWatchers => WatcherNameList;
    public static IEnumerable<WatcherTimerPair> AvailableWatcherPairs => Watchers;

    private static readonly List<string> WatcherNameList = new();
    private static readonly ConcurrentQueue<Func<CancellationToken, Task>> ActionQueue = new();
    private static readonly List<WatcherTimerPair> Watchers = new();
    private static readonly ConcurrentDictionary<int, string> TaskDict = new();

    public static void AddWatcher(Type watcherType)
    {
        IWebWatcher watcher;
        if (!LoadedTypes.ContainsKey(watcherType))
        {
            watcher = (IWebWatcher)Activator.CreateInstance(watcherType)!;

            var enable = WatcherSettings.Current.WatcherEnableList;
            if (!enable.ContainsKey(watcher.Name))
                enable.Add(watcher.Name, true);

            if (!enable[watcher.Name])
            {
                Log.Information($"{watcher.Name} is disabled, skipping...");
                return;
            }
            Log.Information($"Adding {watcher.Name} to active Watchers");

            LoadedTypes.Add(watcherType, watcher);
        }
        else
            return;

        if (!watcher.Validate(out var msg))
        {
            Log.Error($"Failed to load watcher {watcher.Name}: {msg}");
            return;
        }

        WatcherTimerPair pair = new(
            watcher,
            pair => ActionQueue.Enqueue(ct =>
            {
                if (!Statistics.TotalRuns.ContainsKey(pair.Name))
                    Statistics.TotalRuns.Add(pair.Name, 0);
                Statistics.TotalRuns[pair.Name]++;
                var t = pair.Watcher.Check();
                TaskDict[t.Id] = pair.Name;
                return t;
            }));

        Watchers.Add(pair);
        WatcherNameList.Add(pair.Name);
        ActionQueue.Enqueue(ct =>
        {
            var t = pair.Watcher.FirstCheck();
            TaskDict[t.Id] = pair.Name;
            return t;
        });
        pair.Timer.Start();
    }

    public static void ForceCheck(string watcher)
    {
        var w = Watchers.FirstOrDefault(s => s.Name == watcher);
        if (w is null)
            throw new ArgumentException($"There isn't a watcher with the name of {watcher}", nameof(watcher));
        w.Action(w);
    }

    public static void RemoveWatcher(IWebWatcher watcher)
    {
        LoadedTypes.Remove(watcher.GetType());

        var i = Watchers.FindIndex(p => p.Watcher.Equals(watcher));
        var w = Watchers[i];
        Watchers.RemoveAt(i);
        WatcherNameList.Remove(w.Name);
        w.Stop();
    }

    public static void RemoveWatcher(Type watcherType)
    {
        if (LoadedTypes.ContainsKey(watcherType))
            RemoveWatcher(LoadedTypes[watcherType]);
        throw new ArgumentException("There's not a loaded watcher with the given type", nameof(watcherType));
    }

    public static ServiceStatistics Statistics { get; } = new();

    public static async Task Active(CancellationToken token)
    {
        await Task.Yield();
        List<Task> tasks = new();

        Log.Information("Starting WatcherService");
        while (!token.IsCancellationRequested)
        {
            try
            {
                var throttletask = Task.Delay(250, CancellationToken.None);

                var cancel = new CancellationTokenSource(10_000);
                for (int i = 0; i < MaxTasks && !ActionQueue.IsEmpty; i++)
                    if (ActionQueue.TryDequeue(out var task))
                        tasks.Add(task(cancel.Token));

                //--
                Statistics.TotalTasksAwaited += (ulong)tasks.Count;
                Statistics.TasksPerLoop.Add(tasks.Count);

                foreach (var task in tasks)
                {
                    try
                    {
                        await task;
                    }
                    catch (TimeoutException)
                    {
                        TaskDict.TryGetValue(task.Id, out var v);
                        Log.Error("One or more task took too long to complete. The task belongs to watcher {watcher}", task.Id, v ?? "Unknown");
                    }
                    catch (TaskCanceledException)
                    {
                        TaskDict.TryGetValue(task.Id, out var v);
                        Log.Error("One or more task took too long to complete. The task belongs to watcher {watcher}", task.Id, v ?? "Unknown");
                    }
                    finally
                    {
                        TaskDict.TryRemove(task.Id, out _);
                    }
                }

                tasks.Clear();

                if (throttletask.IsCompleted)
                    Statistics.OverworkedLoops++;
                else
                    await throttletask;
            }
            catch (Exception e)
            {
                Log.Fatal(e, "Unexpected exception thrown in WatcherService");
                throw;
            }
        }
        Log.Information("Termination Signal Received, Stopping WatcherService");
    }

    private static readonly Dictionary<Type, IWebWatcher> LoadedTypes = new();

    /// <summary>
    /// Scours the current executing assembly searching for types decorated with the WatcherAttribute, and adds them automatically
    /// </summary>
    public static void LoadWatchers()
    {
        Type? curtype = null;
        try
        {
            foreach (var ty in ReflectionCollectionMethods.GetAllTypesWithAttributeInAssemblies(typeof(WatcherAttribute), false, AppDomain.CurrentDomain.GetAssemblies()))
            {
                curtype = ty;
                AddWatcher(ty);
            }
        }
        catch (Exception e)
        {
            throw new TypeLoadException($"All classes attributed with WatcherAttribute must not be generic, abstract, or static, must have a parameterless constructor, and must implement IWebWatcher directly or indirectly. WatcherAttribute is not inheritable. Check inner exception for more details. Type that caused the exception: {curtype}", e);
        }
    }
}
