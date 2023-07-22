using DiegoG.Utilities;
using DiegoG.Utilities.Basic;
using DiegoG.Utilities.IO;
using DiegoG.Utilities.Reflection;
using DiegoG.Utilities.Settings;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using File = System.IO.File;

namespace DiegoG.WebWatcher;

public static class SubscriptionService
{
    /// <summary>
    /// The maximum amount of tasks per loop
    /// </summary>
    public static uint MaxTasks { get; set; } = 5;

    public class SubscriptionInfo
    {
        public readonly ISubscription Subscription;
        public readonly Task Timer;
        public readonly string Name;
        public readonly Action<SubscriptionInfo> Action;
        public readonly LinkedList<ChatId> Subscribers;

        public bool Pause { get; set; }

        private bool Cancel = false;

        public void Stop()
            => Cancel = true;

#if DEBUG
        private static int i = 0;
        private readonly int id;
#endif
        public SubscriptionInfo(ISubscription subscription, Action<SubscriptionInfo> action)
        {
#if DEBUG
            id = i++;
            Log.Verbose($"New SubscriptionInfo created, ID: {id}");
#endif

            Subscribers = new();
            Subscription = subscription;
            Action = action;
            Name = subscription.Name;

            Timer = new Task(
                async () =>
                {
                    while (!Cancel)
                    {
                        await Task.Delay(subscription.Interval);
                        if (!Pause)
                            Action(this);
                    }
                }, 
                CancellationToken.None, 
                TaskCreationOptions.LongRunning
            );
        }
#if DEBUG
        ~SubscriptionInfo()
        {
            Log.Verbose($"SubscriptionInfo deleted, ID: {id}");
        }
#endif
    }

    public static IEnumerable<string> AvailableSubscriptions => SubscriptionNameList;
    public static IEnumerable<SubscriptionInfo> AvailableSubscriptionInfo => Subscriptions;

    private static readonly List<string> SubscriptionNameList = new();
    private static readonly ConcurrentQueue<Func<Task>> ActionQueue = new();
    private static readonly List<SubscriptionInfo> Subscriptions = new();
    private static readonly ConcurrentDictionary<int, string> TaskDict = new();
    
    public static void AddSubscription(Type subscriptionType)
    {
        ISubscription sub;
        if (!LoadedTypes.ContainsKey(subscriptionType))
        {
            sub = (ISubscription)Activator.CreateInstance(subscriptionType)!;

            var enable = Settings<WatcherSettings>.Current.SubscriptionEnableList;
            if (!enable.ContainsKey(sub.Name))
                enable.Add(sub.Name, true);

            if (!enable[sub.Name])
            {
                Log.Information($"{sub.Name} is disabled, skipping...");
                return;
            }
            Log.Information($"Adding {sub.Name} to active Subscriptions");

            LoadedTypes.Add(subscriptionType, sub);
        }
        else
            return;

        if(!sub.Validate(out var msg))
        {
            Log.Error($"Failed to load watcher {sub.Name}: {msg}");
            return;
        }

        SubscriptionInfo subs = new(
            sub,
            pair => ActionQueue.Enqueue(() =>
            {
                if (!Statistics.TotalRuns.ContainsKey(pair.Name))
                    Statistics.TotalRuns.Add(pair.Name, 0);
                Statistics.TotalRuns[pair.Name]++;
                var t = pair.Subscription.Report(pair.Subscribers);
                TaskDict[t.Id] = pair.Name;
                return t;
            }));

        Subscriptions.Add(subs);
        SubscriptionNameList.Add(subs.Name);
        subs.Timer.Start();
    }

    public static void ForceCheck(string watcher)
    {
        var w = Subscriptions.FirstOrDefault(s => s.Name == watcher) ?? throw new ArgumentException($"There isn't a watcher with the name of {watcher}", nameof(watcher));
        w.Action(w);
    }

    public static void RemoveSubscription(ISubscription sub)
    {
        LoadedTypes.Remove(sub.GetType());

        var i = Subscriptions.FindIndex(p => p.Subscription.Equals(sub));
        var w = Subscriptions[i];
        Subscriptions.RemoveAt(i);
        SubscriptionNameList.Remove(w.Name);
        w.Stop();
    }

    public static void RemoveSubscription(Type subType)
    {
        if (LoadedTypes.ContainsKey(subType))
            RemoveSubscription(LoadedTypes[subType]);
        throw new ArgumentException("There's not a loaded watcher with the given type", nameof(subType));
    }

    public static ServiceStatistics Statistics { get; } = new();

    public static async Task Active(CancellationToken token)
    {
        AsyncTaskManager tasks = new();

        Log.Information("Loading subscriber data");

        {
            var subs = LoadSubscriberData();
            var dict = subs.ToDictionary(x => x.SubscriptionName, y => y.Subscribers);
            foreach (var subscr in AvailableSubscriptionInfo)
            {
                if (dict.TryGetValue(subscr.Name, out var subscribers))
                {
                    subscr.Subscribers.Clear();
                    foreach (var subscriber in subscribers)
                        subscr.Subscribers.AddLast(subscriber);
                }
            }
        }

        Log.Information("Starting SubscriptionService");
        while (!token.IsCancellationRequested)
        {
            var throttletask = Task.Delay(250, CancellationToken.None);

            for (int i = 0; i < MaxTasks && !ActionQueue.IsEmpty; i++)
                if (ActionQueue.TryDequeue(out var task))
                    tasks.Add(task());

            //--
            Statistics.TotalTasksAwaited += (ulong)tasks.Count;
            Statistics.AverageTasksPerLoop.AddSample(tasks.Count);

            foreach (var t in tasks)
                await t.AwaitWithTimeout(5000, ifError: () =>
                {
                    TaskDict.TryGetValue(t.Id, out var v);
                    Log.Error("Task #{taskId} took too long to complete. Task #{taskId} belongs to subscription {subscription}", t.Id, v ?? "Unknown");
                });
            tasks.Clear();

            if (throttletask.IsCompleted)
                Statistics.OverworkedLoops++;
            else
                await throttletask;
        }
        Log.Information("Termination Signal Received, Stopping SubscriptionService");
    }

    private readonly static Dictionary<Type, ISubscription> LoadedTypes = new();

    /// <summary>
    /// Scours the current executing assembly searching for types decorated with the SubscriptionAttribute, and adds them automatically
    /// </summary>
    public static void LoadSubscriptions()
    {
        Type? curtype = null;
        try
        {
            foreach (var ty in ReflectionCollectionMethods.GetAllTypesWithAttributeInAssemblies(typeof(SubscriptionAttribute), false, AppDomain.CurrentDomain.GetAssemblies()))
            {
                curtype = ty;
                AddSubscription(ty);
            }
        }
        catch (Exception e)
        {
            throw new TypeLoadException($"All classes attributed with SubscriptionAttribute must not be generic, abstract, or static, must have a parameterless constructor, and must implement ISubscription directly or indirectly. SubscriptionAttribute is not inheritable. Check inner exception for more details. Type that caused the exception: {curtype}", e);
        }
    }

    public static void SaveSubscriberData()
    {
        try
        {
            using var fstream = File.Open(Directories.InData("subscribers.json"), FileMode.Create);
            JsonSerializer.Serialize(fstream, AvailableSubscriptionInfo.Select(x => new SubscriptionWriteInfoBuffer(x.Name, x.Subscribers.Select(x => x.Identifier))));
        }
        catch (Exception e)
        {
            Log.Error(e, "Could not save subscriber data");
        }
    }

    private static SubscriptionReadInfoBuffer[] LoadSubscriberData()
    {
        var fname = Directories.InData("subscribers.json");
        if (File.Exists(fname) is false)
            return Array.Empty<SubscriptionReadInfoBuffer>();

        using var fstream = File.Open(fname, FileMode.Open);
        return JsonSerializer.Deserialize<SubscriptionReadInfoBuffer[]>(fstream) ?? Array.Empty<SubscriptionReadInfoBuffer>();
    }

    private readonly record struct SubscriptionWriteInfoBuffer(string SubscriptionName, IEnumerable<long> Subscribers);
    private readonly record struct SubscriptionReadInfoBuffer(string SubscriptionName, long[] Subscribers);
}