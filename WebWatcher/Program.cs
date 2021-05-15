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

namespace DiegoG.WebWatcher
{
    public static class Program
    {
        public static TimeSpan RunningTime => RunningTimeWatch.Elapsed;
        public readonly static Version Version = new(0, 0, 7, 6);

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
                throw new InvalidDataException("Settings file is invalid. Please fill out the BotAPIKey field");

            ExtensionLoader.Initialize(Directories.Extensions);

            while (true)
            {
                try
                {
                    Log.Information("Initializing bot");
                    OutputBot.Initialize();
                    break;
                }
                catch (HttpRequestException)
                {
                    Log.Warning($"Caught an HTTP Request Exception, waiting {NetworkWait.TotalSeconds} seconds and trying again");
                    OutputBot.StopReceiving();
                    await Task.Delay(NetworkWait);
                }
            }

            var settings = Settings<WatcherSettings>.Current;

            var logconf = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(settings.ConsoleLogEventLevel)
                .WriteTo.File(Directories.InLogs(".log"), rollingInterval: RollingInterval.Hour, restrictedToMinimumLevel: settings.FileLogEventLevel);

            if (settings.LogChatId is not null)
                logconf.WriteTo.TelegramBot(settings.LogChatId, OutputBot.Client, settings.BotLogEventLevel);

            Log.Logger = logconf.CreateLogger();

            ExtensionLoader.Load(ExtensionLoader.EnumerateUnloadedAssemblies().Where(s =>
            {
                s = Path.GetFileName(s);
                if (!settings.ExtensionsEnable.ContainsKey(s))
                    settings.ExtensionsEnable.Add(s, false);

                var r = settings.ExtensionsEnable[s];

                Log.Information(r ? $"Extension {s} is enabled, queuing." : $"Extension {s} is disabled, ignoring.");

                return settings.ExtensionsEnable[s];
            }));

            BotCommandProcessor.Initialize(OutputBot.SendTextMessage);
            OutputBot.OnMessage += BotCommandProcessor.Bot_OnMessage;

            Service.LoadWatchers();

            Settings<WatcherSettings>.SaveSettings();

            ProgramHost = CreateHostBuilder(args).Build();

            while (true)
            {
                try
                {
                    await OutputBot.Client.SetMyCommandsAsync(BotCommandProcessor.CommandList.AvailableCommands);
                    OutputBot.StartReceiving(new[] { UpdateType.Message });
                    ProgramHost.Run();
                }
                catch (HttpRequestException)
                {
                    Log.Warning($"Caught an HTTP Request Exception, waiting {NetworkWait.TotalSeconds} seconds and trying again");
                    OutputBot.StopReceiving();
                    await Task.Delay(NetworkWait);
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

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) => services.AddHostedService<Worker>());
    }
}
