using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace DiegoG.WebWatcher;

public class WatcherSettings
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public WatcherSettings()
    {
#if DEBUG
        FileLogEventLevel = Serilog.Events.LogEventLevel.Verbose;
#endif
    }

    private static WatcherSettings? settings;
    public static WatcherSettings Current => settings ?? throw new InvalidOperationException("Current has not been assigned yet. Be sure to call LoadFromFile beforehand.");

    private static string? file;
    public static string SettingsFile
    {
        get => file ?? throw new InvalidOperationException("SettingsFile has not been assigned yet. Either set it before calling LoadFromFile or SaveToFile, or call these methods with a non-null string");
        set
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (file == value) return;
            try
            {
                var x = Path.GetDirectoryName(value) ?? throw new ArgumentException($"The file {value} does not have a valid directory name", nameof(value));
                Directory.CreateDirectory(x);
                if (File.Exists(value))
                {
                    var stream = File.Open(value, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                    stream.Dispose();
                }
                else
                    File.Create(value);

                file = value;
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Could not verify or create a directory for the file {value}", nameof(value), e);
            }
        }
    }

    public static void LoadFromFile(string? file = null)
    {
        using var stream = File.OpenRead(string.IsNullOrWhiteSpace(file) ? SettingsFile : (SettingsFile = file));
        var x = JsonSerializer.Deserialize<WatcherSettings>(stream);
        settings = x!;
    }

    public static void SaveToFile(string? file = null)
    {
        using var stream = File.Open(string.IsNullOrWhiteSpace(file) ? SettingsFile : (SettingsFile = file), FileMode.Create, FileAccess.Write);
        JsonSerializer.Serialize(stream, Current, JsonOptions);
    }

    public static async Task LoadFromFileAsync(string? file = null)
    {
        using var stream = File.OpenRead(string.IsNullOrWhiteSpace(file) ? SettingsFile : (SettingsFile = file));
        var x = await JsonSerializer.DeserializeAsync<WatcherSettings>(stream);
        settings = x!;
    }

    public static Task SaveToFileAsync(string? file = null)
    {
        using var stream = File.Open(string.IsNullOrWhiteSpace(file) ? SettingsFile : (SettingsFile = file), FileMode.Create, FileAccess.Write);
        return JsonSerializer.SerializeAsync(stream, Current, JsonOptions);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public Serilog.Events.LogEventLevel FileLogEventLevel { get; set; } = Serilog.Events.LogEventLevel.Debug;
    public Serilog.Events.LogEventLevel BotLogEventLevel { get; set; } = Serilog.Events.LogEventLevel.Information;
    public Serilog.Events.LogEventLevel ConsoleLogEventLevel { get; set; } = Serilog.Events.LogEventLevel.Debug;
    public string? BotAPIKey { get; set; }

    public string? VersionName { get; set; } = "";

    public long LogChatId { get; set; } = 0;

    public IDictionary<string, bool> SubscriptionEnableList { get; set; } = new Dictionary<string, bool>();
    public IDictionary<string, bool> WatcherEnableList { get; set; } = new Dictionary<string, bool>();

    public IDictionary<string, bool> ExtensionsEnable { get; set; } = new Dictionary<string, bool>();

    public string? RestartCommand { get; set; }
    public string? RestartCommandArguments { get; set; }
}
