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

namespace DiegoG.WebWatcher
{
    public static class Program
    {
        public static TimeSpan RunningTime => RunningTimeWatch.Elapsed;
        public readonly static Version Version = new(0, 0, 4, 2);

        private readonly static Stopwatch RunningTimeWatch = new();
        public static void Main(string[] args)
        {
            RunningTimeWatch.Start();

            Settings<WatcherSettings>.Initialize(Directories.Configuration, "settings.cfg");
            if (Settings<WatcherSettings>.Current.BotAPIKey is null)
                throw new InvalidDataException("Settings file is invalid. Please fill out the BotAPIKey field");
#if !DEBUG
            try
            {
#endif
            OutputBot.Initialize();

            BotCommandProcessor.Initialize(OutputBot.SendTextMessage);
            OutputBot.OnMessage += BotCommandProcessor.Bot_OnMessage;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
#if !DEBUG
                .WriteTo.LocalSyslog("WebWatcher")
#endif
                .WriteTo.Console(Serilog.Events.LogEventLevel.Debug)
                .WriteTo.File(Directories.InLogs(".log"), rollingInterval: RollingInterval.Hour, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose)
                .WriteTo.TelegramBot(-1001445070822, OutputBot.Client, Serilog.Events.LogEventLevel.Debug)
                .CreateLogger();

                Service.AddWatcher(new WitchCultWatcher());
                Service.AddWatcher(new SelfWatcher());
                Service.AddWatcher(new StatusWatcher());

            OutputBot.StartReceiving(new[] { UpdateType.Message });
            CreateHostBuilder(args).Build().Run();
#if !DEBUG
            }catch(Exception e)
            {
                Log.Fatal($"Unhandled Exception Caught at Main {e.Message}:\n{e}");
                Log.CloseAndFlush();
                Console.WriteLine($"Unhandled Exception Caught at Main {e.Message}:\n{e}");
                throw;
            }
#endif
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) => services.AddHostedService<Worker>());
    }
}
