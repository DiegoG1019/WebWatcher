using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DiegoG.Utilities.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Exceptions;

namespace DiegoG.WebWatcher;

public static class Program
{
    public static HashSet<string> LoadedAssemblies { get; } = new();
    public static TimeSpan RunningTime => RunningTimeWatch.Elapsed;
    public static readonly Version Version = new(0, 13, 0);
    public static readonly Version MinimumSupported = new(0, 13, 0);

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private static IHost ProgramHost;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private static readonly Stopwatch RunningTimeWatch = new();
    private static readonly TimeSpan NetworkWait = TimeSpan.FromMinutes(.5);

    public static async Task Main(string[] args)
    {
        RunningTimeWatch.Start();

        await WatcherSettings.LoadFromFileAsync(Directories.InConfiguration("settings.cfg.json"));

        if (WatcherSettings.Current.BotAPIKey is null)
            throw new InvalidDataException($"Settings file is invalid. Please fill out the BotAPIKey field in {Directories.InConfiguration("settings.cfg.json")}");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console(Serilog.Events.LogEventLevel.Verbose)
            .WriteTo.LocalSyslog("DiegoG.WebWatcher")
            .CreateLogger();

        Log.Information("Configurations Directory: {directory}", Directories.Configuration);
        Log.Information("Data Directory: {directory}", Directories.Data);
        Log.Information("Logs Directory: {directory}", Directories.Logs);
        Log.Information("Extensions Directory: {directory}", Directories.Extensions);

#if DEBUG
        var report = new global::WebWatcher.HouseVoltage.VoltageReport(14);
#endif

        {
            Log.Information("Ensuring all referenced assemblies are loaded");

            while (true)
            {
                bool newLoaded = false;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetReferencedAssemblies()))
                {
                    if (LoadedAssemblies.Add(asm.FullName))
                    {
                        VerifyExtensionAssembly(AppDomain.CurrentDomain.Load(asm.FullName));
                        newLoaded = true;
                    }
                }
                if (newLoaded is false) break;
            }

            Log.Information("All referenced assemblies have been loaded");
        }

        ExtensionLoader.AssemblyLoaded += ExtensionAssemblyLoaded;
        ExtensionLoader.Initialize(Directories.Extensions);

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
            catch (InvalidDataException)
            {
                throw;
            }
            catch (IOException)
            {
                throw;
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (RequestException)
            {
                throw;
            }
            catch (Exception e)
            {
                await WaitAndTryAgain(e);
            }
        }

        var settings = WatcherSettings.Current;

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

        await WatcherSettings.SaveToFileAsync();

        {
            var builder = CreateHostBuilder(args);
            builder.UseSerilog();
            ProgramHost = builder.Build();
        }

        {
            var setts = WatcherSettings.Current;
            Log.Information($"Started Running WebWatcher v.{Program.Version}{(setts.VersionName is not null ? $" - {setts.VersionName}" : "")}");
        }
        while (true)
        {
            try
            {
                await ProgramHost.RunAsync();
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

        static async Task WaitAndTryAgain(Exception e)
        {
            Log.Warning($"Caught: {e.GetType().Name}: {e.Message}\nwaiting {NetworkWait.TotalSeconds} seconds and trying again");
            await Task.Delay(NetworkWait);
        }
    }

    private static void ExtensionAssemblyLoaded(AssemblyName loadedAssembly)
    {
        if (LoadedAssemblies.Add(loadedAssembly.FullName))
            VerifyExtensionAssembly(AppDomain.CurrentDomain.Load(loadedAssembly.FullName));
    }

    /// <summary>
    /// Verifies that <paramref name="asm"/> is an extension assembly and if so, that it's compatible with the current version of WebWatcher
    /// </summary>
    /// <remarks>
    /// If the assembly is found to be an extension assembly, but fails verification, an <see cref="InvalidDataException"/> is thrown
    /// </remarks>
    /// <param name="asm"></param>
    /// <returns><see langword="true"/>if the assembly is an extension assembly and passed verification. <see langword="false"/> if it's not an extension assembly.</returns>
    public static bool VerifyExtensionAssembly(Assembly asm)
    {
        var attr = asm.GetCustomAttribute<WatcherExtensionAttribute>();
        if (attr is not null)
        {
            if (attr.MinimumMajorVersion is int minmajor and >= 0)
            {
                if (minmajor > Program.Version.Major)
                    ThrowForNotSupported();

                if (attr.MinimumMinorVersion is int minminor and >= 0 && minminor > Program.Version.Minor)
                    ThrowForNotSupported();
            }

            if (attr.TargetMajorVersion is int targetmajor and >= 0)
            {
                if (targetmajor < Program.MinimumSupported.Major)
                    ThrowForNotSupported();

                if (attr.TargetMinorVersion is int targetminor and >= 0 && targetminor < Program.MinimumSupported.Minor)
                    ThrowForNotSupported();
            }

            return true;
        }
        return false;

        static void ThrowForNotSupported()
            => throw new InvalidDataException($"The minimum major version for this extension does not support the current version.");
    }

    public static void LoadEnabledExtensions()
    {
        var settings = WatcherSettings.Current;
        ExtensionLoader.Load(ExtensionLoader.EnumerateUnloadedAssemblies().Where(s =>
        {
            s = Path.GetFileName(s);
            if (!settings.ExtensionsEnable.ContainsKey(s))
                settings.ExtensionsEnable.Add(s, false);

            var r = settings.ExtensionsEnable[s];

            Log.Information(r ? $"Extension {s} is enabled, queuing." : $"Extension {s} is disabled, ignoring.");

            return r;
        }));
    }

    [ThreadStatic]
    private static StringBuilder? StringBuilder;
    public static StringBuilder GetSharedStringBuilder() => (StringBuilder ??= new()).Clear();

    public static void LoadExtensions(params string[] names)
    {
        var ext = string.Join(" ,", names);
        Log.Information($"Attempting to load extensions: {ext}");
        var settings = WatcherSettings.Current;

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