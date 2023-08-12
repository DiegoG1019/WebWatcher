using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using DiegoG.Utilities.Reflection;
using Serilog;
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
        private readonly Action<IEnumerable<ChatId>, SubscriptionInfo> Action;
        private readonly HashSet<ChatId> Subscribers;

        public IEnumerable<ChatId> SubscriberList => Subscribers;

        public bool Pause { get; set; }

        private bool Cancel = false;

        public void Stop()
            => Cancel = true;

        public async Task<bool> AddSubscriber(ChatId chatId)
        {
            lock (Subscribers)
            {
                if (Subscribers.Add(chatId) is false)
                    return false;
                SaveSubscriberData();
            }
            await Subscription.Subscribed(chatId);
            return true;
        }

        public async Task<bool> RemoveSubscriber(ChatId chatId)
        {
            lock (Subscribers)
            {
                if (Subscribers.Remove(chatId) is false)
                    return false;
                SaveSubscriberData();
            }
            await Subscription.Unsubscribed(chatId);
            return true;
        }

        public async Task ClearSubscribers()
        {
            foreach (var sub in Subscribers)
                await RemoveSubscriber(sub);
        }

        public void Execute()
        {
            Action(Subscribers, this);
        }

#if DEBUG
        private static int i = 0;
        private readonly int id;
#endif
        public SubscriptionInfo(ISubscription subscription, Action<IEnumerable<ChatId>, SubscriptionInfo> action)
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
                            Execute();
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
    private static readonly ConcurrentQueue<Func<CancellationToken, Task>> ActionQueue = new();
    private static readonly List<SubscriptionInfo> Subscriptions = new();
    private static readonly ConcurrentDictionary<int, string> TaskDict = new();

    public static void AddSubscription(Type subscriptionType)
    {
        ISubscription sub;
        if (!LoadedTypes.ContainsKey(subscriptionType))
        {
            sub = (ISubscription)Activator.CreateInstance(subscriptionType)!;

            var enable = WatcherSettings.Current.SubscriptionEnableList;
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

        if (!sub.Validate(out var msg))
        {
            Log.Error($"Failed to load watcher {sub.Name}: {msg}");
            return;
        }

        SubscriptionInfo subs = new(
            sub,
            (sublist, pair) => ActionQueue.Enqueue(ct =>
            {
                if (!Statistics.TotalRuns.ContainsKey(pair.Name))
                    Statistics.TotalRuns.Add(pair.Name, 0);
                Statistics.TotalRuns[pair.Name]++;
                var t = pair.Subscription.Report(sublist);
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
        w.Execute();
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
        await Task.Yield();
        List<Task> tasks = new();

        Log.Information("Loading subscriber data");

        {
            var subs = LoadSubscriberData();
            var dict = subs.ToDictionary(x => x.SubscriptionName, y => y.Subscribers);
            foreach (var subscr in AvailableSubscriptionInfo)
            {
                if (dict.TryGetValue(subscr.Name, out var subscribers))
                {
                    await subscr.ClearSubscribers();
                    foreach (var subscriber in subscribers)
                        await subscr.AddSubscriber(subscriber.ToChatId());
                }
            }
        }

        Log.Information("Starting SubscriptionService");
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
                        Log.Error("One or more task took too long to complete. The task belongs to subscription {subscription}", task.Id, v ?? "Unknown");
                    }
                    catch (TaskCanceledException)
                    {
                        TaskDict.TryGetValue(task.Id, out var v);
                        Log.Error("One or more task took too long to complete. The task belongs to subscription {subscription}", task.Id, v ?? "Unknown");
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
                Log.Fatal(e, "Unexpected exception thrown in SubscriptionService");
                throw;
            }
        }
        Log.Information("Termination Signal Received, Stopping SubscriptionService");
    }

    private static readonly Dictionary<Type, ISubscription> LoadedTypes = new();

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

    private static Task SaveSubscriberData()
    {
        try
        {
            using var fstream = File.Open(Directories.InData("subscribers.json"), FileMode.Create);
            return JsonSerializer.SerializeAsync(fstream, AvailableSubscriptionInfo.Select(x => new SubscriptionWriteInfoBuffer(x.Name, x.SubscriberList.Select(x => new ChatIdBuffer(x)))));
        }
        catch (Exception e)
        {
            Log.Error(e, "Could not save subscriber data");
            return Task.CompletedTask;
        }
    }

    private static SubscriptionReadInfoBuffer[] LoadSubscriberData()
    {
        var fname = Directories.InData("subscribers.json");
        if (File.Exists(fname) is false)
            return Array.Empty<SubscriptionReadInfoBuffer>();

        try
        {
            using var fstream = File.Open(fname, FileMode.Open);
            return JsonSerializer.Deserialize<SubscriptionReadInfoBuffer[]>(fstream) ?? Array.Empty<SubscriptionReadInfoBuffer>();
        }
        catch(Exception e)
        {
            ;
            throw;
        }
    }

    private readonly record struct SubscriptionWriteInfoBuffer(string SubscriptionName, IEnumerable<ChatIdBuffer> Subscribers);
    private readonly record struct SubscriptionReadInfoBuffer(string SubscriptionName, ChatIdBuffer[] Subscribers);
    private readonly struct ChatIdBuffer
    {
        public string Identifier { get; }
        public bool IsUsername { get; }

        public ChatIdBuffer(ChatId chatId)
        {
            ArgumentNullException.ThrowIfNull(chatId);
            if (chatId.Identifier is long iden)
            {
                Identifier = iden.ToString();
                IsUsername = false;
            }
            else
            {
                Identifier = chatId.Username!;
                IsUsername = true;
            }
        }

        [JsonConstructor]
        public ChatIdBuffer(string identifier, bool isUsername)
        {
            Identifier = identifier;
            IsUsername = isUsername;
        }

        public ChatId ToChatId()
            => IsUsername ? new ChatId(Identifier) : new ChatId(long.Parse(Identifier));
    }   
}