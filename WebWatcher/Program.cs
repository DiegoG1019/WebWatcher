using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using System.IO;
using DiegoG.Utilities.Settings;
using System.Diagnostics;
using DiegoG.TelegramBot;
using Telegram.Bot.Types.Enums;
using System.Net.Http;
using DiegoG.Utilities.Reflection;
using Telegram.Bot.Exceptions;
using System.Text;
using System.Runtime.CompilerServices;

namespace DiegoG.WebWatcher
{
    public static class Program
    {
        public static TimeSpan RunningTime => RunningTimeWatch.Elapsed;
        public readonly static Version Version = new(0, 12, 1);

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private static IHost ProgramHost;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private readonly static Stopwatch RunningTimeWatch = new();
        private readonly static TimeSpan NetworkWait = TimeSpan.FromMinutes(.5);

        public static async Task Main(string[] args)
        {
            RunningTimeWatch.Start();

            Settings<WatcherSettings>.Initialize(Directories.Configuration, "settings.cfg", true, null, s => { });

            if (Settings<WatcherSettings>.Current.BotAPIKey is null)
                throw new InvalidDataException($"Settings file is invalid. Please fill out the BotAPIKey field in {Directories.InConfiguration("settings.cfg.json")}");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(Serilog.Events.LogEventLevel.Verbose)
                .WriteTo.LocalSyslog("DiegoG.WebWatcher")
                .CreateLogger();
            
            ExtensionLoader.Initialize(Directories.Extensions);

            static async Task WaitAndTryAgain(Exception e)
            {
                Log.Warning($"Caught: {e.GetType().Name}: {e.Message}\nwaiting {NetworkWait.TotalSeconds} seconds and trying again");
                await Task.Delay(NetworkWait);
            }
            while (true)
            {
                try
                {
                    Log.Information("Initializing bot");
                    OutputBot.Initialize();
                    OutBot.Processor = OutputBot.Client;
                    OutBot.GetIfAdmin_ = OutputBot.GetAdmin;
                    break;
                }
                catch(InvalidDataException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    await WaitAndTryAgain(e);
                }
            }

            var settings = Settings<WatcherSettings>.Current;

            var logconf = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(settings.ConsoleLogEventLevel)
                .WriteTo.File(Directories.InLogs(".log"), rollingInterval: RollingInterval.Hour, restrictedToMinimumLevel: settings.FileLogEventLevel);

            //if (settings.LogChatId is not 0)
            //    logconf.WriteTo.TelegramBot(settings.LogChatId, OutputBot.Client, settings.BotLogEventLevel, settings.VersionName);

            Log.Logger = logconf.CreateLogger();

            LoadEnabledExtensions();

            WatcherService.LoadWatchers();
            SubscriptionService.LoadSubscriptions();
            OutputBot.Client.LoadNewCommands(AppDomain.CurrentDomain.GetAssemblies());

            OutputBot.Client.MessageQueue.ApiSaturationLimit = 20;

            Settings<WatcherSettings>.SaveSettings();

            {
                var builder = CreateHostBuilder(args);
                builder.UseSerilog();
                ProgramHost = builder.Build();
            }

            {
                var setts = Settings<WatcherSettings>.Current;
                Log.Information($"Started Running WebWatcher v.{Program.Version} {(setts.VersionName is not null ? $"- {setts.VersionName}" : "")}");
            }
            while (true)
            {
                try
                {
                    ProgramHost.Run();
                    Log.Information("Application succesfully shut down");
                    return;
                }
                catch (HttpRequestException)
                {
                    Log.Warning($"Caught an HTTP Request Exception, waiting {NetworkWait.TotalSeconds} seconds and trying again");
                    await Task.Delay(NetworkWait);
                }
                catch (OperationCanceledException)
                {
                    Log.Information("Application shut down");
                    return;
                }
#if !DEBUG
                catch(Exception e)
                {
                    Log.Fatal($"Unhandled Exception Caught at Main {e.Message}");
                    Log.Fatal(e, e.GetType().Name);
                    Log.CloseAndFlush();
                    Console.WriteLine($"Unhandled Exception Caught at Main {e.Message}:\n{e}");
                    throw;
                }
#endif
            }
        }

        public static void LoadEnabledExtensions()
        {
            var settings = Settings<WatcherSettings>.Current;
            ExtensionLoader.Load(ExtensionLoader.EnumerateUnloadedAssemblies().Where(s =>
            {
                s = Path.GetFileName(s);
                if (!settings.ExtensionsEnable.ContainsKey(s))
                    settings.ExtensionsEnable.Add(s, false);

                var r = settings.ExtensionsEnable[s];

                Log.Information(r ? $"Extension {s} is enabled, queuing." : $"Extension {s} is disabled, ignoring.");

                return settings.ExtensionsEnable[s];
            }));
        }

        [ThreadStatic]
        private static StringBuilder? StringBuilder;
        public static StringBuilder GetSharedStringBuilder() => (StringBuilder ??= new()).Clear();

        public static void LoadExtensions(params string[] names)
        {
            var ext = string.Join(" ,", names);
            Log.Information($"Attempting to load extensions: {ext}");
            var settings = Settings<WatcherSettings>.Current;

            var allowednames = names.Where(s => ExtensionLoader.LoadedExtensions.All(d => d != s));

            foreach (var name in allowednames)
                if (!settings.ExtensionsEnable.ContainsKey(Path.GetFileName(name)))
                    settings.ExtensionsEnable.Add(Path.GetFileName(name), true);

            allowednames = allowednames.Where(s => settings.ExtensionsEnable[Path.GetFileName(s)]);
            ExtensionLoader.Load(allowednames);

            WatcherService.LoadWatchers();
            SubscriptionService.LoadSubscriptions();
            OutputBot.Client.LoadNewCommands(AppDomain.CurrentDomain.GetAssemblies());

            Log.Information($"Succesfully loaded extensions: {ext}");

            OutBot.EnqueueAction(b => b.SetMyCommandsAsync(OutBot.Processor.CommandList.AvailableCommands));
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) => services.AddHostedService<Worker>());
    }
}