using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using DiegoG.WebWatcher;

namespace WebWatcher.HouseVoltage;

public partial class VoltageTracker
{
    private readonly ConcurrentQueue<(ushort Peak, ushort Valley)> reports = new();
    private readonly string ConfigFile;

    private readonly Thread ReportCompiler;
    private readonly Thread ReportReader;

    private CircularList<VoltageReport> History { get; } = new CircularList<VoltageReport>(60);

    private DateTime ConfigFileLastUpdate;

    private SerialReader Reader;

    public VoltageReport Latest { get; private set; }
    public IReadOnlyCollection<VoltageReport> Reports => History;

    public event Action<VoltageReport>? IncomingNewReport;

    public static VoltageTracker Tracker { get; } = new();

    private VoltageTracker()
    {
        ReportCompiler = new(CompileReportsWorker)
        {
            IsBackground = true,
            Priority = ThreadPriority.AboveNormal
        };

        ReportReader = new(ReadReportsWorker)
        {
            IsBackground = true,
            Priority = ThreadPriority.Highest
        };

        Span<SerialReader.PinConfiguration> pinSpan = stackalloc SerialReader.PinConfiguration[1];
        ref SerialReader.PinConfiguration pin = ref pinSpan[0];

        var datdir = Directories.InConfiguration(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DiegoG.WebWatcher");
        var datfile = Path.Combine(datdir, "voltagewatcher.cfg");

        Directory.CreateDirectory(datdir);
        if (File.Exists(datfile) is false)
        {
            using var fstream = File.Open(datfile, FileMode.Create);
            pin = new SerialReader.PinConfiguration(11, 29, 31, 13, 15);
            fstream.Write(MemoryMarshal.AsBytes(pinSpan));
        }
        else
        {
            using var fstream = File.OpenRead(datfile);
            fstream.Read(MemoryMarshal.AsBytes(pinSpan));
        }

        ConfigFile = datfile;
        ConfigFileLastUpdate = File.GetLastWriteTime(datfile);
        Reader = new(pin);

        ReportCompiler.Start();
        ReportReader.Start();
    }

    private void UpdatePinConfigFile()
    {
        if (File.GetLastWriteTime(ConfigFile) > ConfigFileLastUpdate)
        {
            Span<SerialReader.PinConfiguration> pinSpan = stackalloc SerialReader.PinConfiguration[1];
            using var fstream = File.OpenRead(ConfigFile);
            fstream.Read(MemoryMarshal.AsBytes(pinSpan));
            Reader = new(pinSpan[0]);
            ConfigFileLastUpdate = File.GetLastWriteTime(ConfigFile);
        }
    }
}

