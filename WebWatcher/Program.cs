﻿using Microsoft.Extensions.DependencyInjection;
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

namespace DiegoG.WebWatcher
{
    public static class Program
    {
        public static TimeSpan RunningTime => RunningTimeWatch.Elapsed;
        public readonly static Version Version = new(0, 0, 5, 2);

        private static IHost ProgramHost;
        private readonly static Stopwatch RunningTimeWatch = new();
        private readonly static TimeSpan NetworkWait = TimeSpan.FromMinutes(.5);
        public static async Task Main(string[] args)
        {
            RunningTimeWatch.Start();


            Settings<WatcherSettings>.Initialize(Directories.Configuration, "settings.cfg", true, null, s =>
            {
                s.EnableList = new Dictionary<string, bool>();
            });

            if (Settings<WatcherSettings>.Current.BotAPIKey is null)
                throw new InvalidDataException("Settings file is invalid. Please fill out the BotAPIKey field");

            Initialize(args);

            while (true)
            {
                try
                {
                    await OutputBot.Client.SetMyCommandsAsync(BotCommandProcessor.CommandList.AvailableCommands);
                    OutputBot.StartReceiving(new[] { UpdateType.Message });
                    ProgramHost.Run();
                }
                catch (HttpRequestException e)
                {
                    Log.Warning($"Caught an HTTP Request Exception, waiting {NetworkWait.TotalSeconds} seconds and trying again");
                    OutputBot.StopReceiving();
                    await Task.Delay(NetworkWait);
                }
#if !DEBUG
                catch(Exception e)
                {
                    Log.Fatal($"Unhandled Exception Caught at Main {e.Message}");
                    Log.Fatal(e);
                    Log.CloseAndFlush();
                    Console.WriteLine($"Unhandled Exception Caught at Main {e.Message}:\n{e}");
                    throw;
                }
#endif
            }
        }


        public static void Initialize(string[] args)
        {
            OutputBot.Initialize();

            BotCommandProcessor.Initialize(OutputBot.SendTextMessage);
            OutputBot.OnMessage += BotCommandProcessor.Bot_OnMessage;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(Serilog.Events.LogEventLevel.Debug)
                .WriteTo.File(Directories.InLogs(".log"), rollingInterval: RollingInterval.Hour, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose)
                .WriteTo.TelegramBot(-1001445070822, OutputBot.Client, Serilog.Events.LogEventLevel.Debug)
                .CreateLogger();

            Service.LoadWatchers();
            ProgramHost = CreateHostBuilder(args).Build();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) => services.AddHostedService<Worker>());
    }
}
