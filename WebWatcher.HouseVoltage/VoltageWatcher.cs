using System.Collections.Concurrent;
using System.Device.Gpio;
using System.Formats.Tar;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DiegoG.TelegramBot.Types;
using DiegoG.Utilities.Collections;
using DiegoG.Utilities.Settings;
using DiegoG.WebWatcher;

namespace WebWatcher.HouseVoltage;

// // This watcher was commented out because its duties were deferred to VoltageTracker, since it needs to be fired much more frequently

//[Watcher]
//public class VoltageWatcher : IWebWatcher
//{
//    public string Name => "VoltageWatcher";
//    public TimeSpan Interval => TimeSpan.FromSeconds(1);

//    private static readonly string ConfigFile;
//    private static DateTime ConfigFileLastUpdate;

//    private static SerialReader Reader;
//    private static readonly byte[] ReaderBuffer = new byte[4];

//    public async Task Check()
//    {
//        if (ConfigFileLastUpdate != File.GetLastWriteTime(ConfigFile))
//            UpdateLastFile();

//        Array.Clear(ReaderBuffer);
//        await Reader.ReadNext(ReaderBuffer);
//        VoltageReport.AddReport(VoltageReading.ReadBytes(ReaderBuffer));
//    }

//    public Task FirstCheck()
//        => Task.CompletedTask;

//    private static void UpdateLastFile()
//    {
//        Span<SerialReader.PinConfiguration> pinSpan = stackalloc SerialReader.PinConfiguration[1];
//        using var fstream = File.OpenRead(ConfigFile);
//        fstream.Read(MemoryMarshal.AsBytes(pinSpan));
//        Reader = new(pinSpan[0]);
//        ConfigFileLastUpdate = File.GetLastWriteTime(ConfigFile);
//    }

//    static VoltageWatcher()
//    {
//        Span<SerialReader.PinConfiguration> pinSpan = stackalloc SerialReader.PinConfiguration[1];
//        ref SerialReader.PinConfiguration pin = ref pinSpan[0];

//        var datdir = Directories.InConfiguration(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DiegoG.WebWatcher");
//        var datfile = Path.Combine(datdir, "voltagewatcher.cfg");

//        Directory.CreateDirectory(datdir);
//        if (File.Exists(datfile) is false)
//        {
//            using var fstream = File.Open(datfile, FileMode.Create);
//            pin = new SerialReader.PinConfiguration(11, 29, 31, 13, 15);
//            fstream.Write(MemoryMarshal.AsBytes(pinSpan));
//        }
//        else
//        {
//            using var fstream = File.OpenRead(datfile);
//            fstream.Read(MemoryMarshal.AsBytes(pinSpan));
//        }

//        ConfigFile = datfile;
//        ConfigFileLastUpdate = File.GetLastWriteTime(datfile);
//        Reader = new(pin);
//    }
//}
