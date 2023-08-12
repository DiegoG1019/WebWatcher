using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Text;
using DiegoG.WebWatcher;
using Serilog;

namespace WebWatcher.HouseVoltage;

public class VoltageTracker
{
    [StructLayout(LayoutKind.Explicit)]
    private struct DataBuffer
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct VoltageBuffer
        {
            public short Max;
            public short Min;
        }

        [FieldOffset(0)]
        public VoltageBuffer Voltage;

        [FieldOffset(0)]
        public uint Raw;
    }

#if DEBUG
    private bool DEBUG_NoPort;
#endif

    private readonly ConcurrentQueue<DataBuffer> reports = new();
    private readonly Thread ReportCompiler;
    private readonly SerialPort Port;
    private readonly string ConfigFile;
    private readonly string ConfigDir;
    private readonly string DefaultComPort;
    private readonly int SampleSize;

    private List<DataBuffer> BufferBack;
    private List<DataBuffer> Buffer;
    private readonly CircularList<VoltageReport> History = new(5000);

    private DateTime ConfigFileLastUpdate;

    public VoltageReport Latest { get; private set; }
    public IReadOnlyCollection<VoltageReport> Reports => History;

    public event Action<VoltageReport>? IncomingNewReport;

    public VoltageTracker(string defaultComPort = "/dev/ttyS0", string comPortFile = "voltagewatcher.cfg", int sampleSize = 200)
    {
        SampleSize = sampleSize > 1 ? sampleSize : throw new ArgumentException("SampleSize must be larger than 1", nameof(sampleSize));
        BufferBack = new(SampleSize);
        Buffer = new(SampleSize);

        DefaultComPort = defaultComPort;

        ReportCompiler = new(CompileReportsWorker)
        {
            IsBackground = true,
            Priority = ThreadPriority.AboveNormal
        };

        ConfigDir = Directories.InConfiguration(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DiegoG.WebWatcher");
        ConfigFile = Path.Combine(ConfigDir, comPortFile);

        Port = new()
        {
            BaudRate = 115200
        };

        Port.DataReceived += Port_DataReceived;
        UpdatePinConfigFile();

        ReportCompiler.Start();
    }

    private readonly byte[] readBuffer = new byte[50 * 1024];
    private readonly char[] charBuffer = new char[50 * 1024];
    private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        var port = ((SerialPort)sender);
        while (port.BytesToRead > 0)
        {
            int read = port.Read(readBuffer, 0, readBuffer.Length);
            Encoding.UTF8.GetChars(readBuffer.AsSpan(0, read), charBuffer);

            int last = 0;
            int i = 0;
            for (; charBuffer[i] is not '\0' && i < readBuffer.Length; i++)
            {
                if (charBuffer[i] is '\n')
                {
                    if (uint.TryParse(charBuffer.AsSpan(last, i - last), NumberStyles.HexNumber, null, out var result))
                        reports.Enqueue(new DataBuffer() { Raw = result });
                    last = i;
                }
            }

            Array.Clear(charBuffer, 0, i);
            Array.Clear(readBuffer, 0, read);
        }
    }

    private void CompileReportsWorker()
    {
        var sw = new Stopwatch();
        sw.Restart();
#if DEBUG
        int DEBUG_RandomFakeReportTicketCount = SampleSize * 4;
        int DEBUG_RandomFakeReportTickets = DEBUG_RandomFakeReportTicketCount;
#endif

        while (true)
        {
#if DEBUG
            if (DEBUG_NoPort)
            {
                if (DEBUG_RandomFakeReportTickets-- <= 0) 
                {
                    DEBUG_RandomFakeReportTickets = DEBUG_RandomFakeReportTicketCount;
                    for (int i = 0; i < SampleSize * 2; i++)
                        ProcessReport(new DataBuffer()
                        {
                            Voltage = new DataBuffer.VoltageBuffer()
                            {
                                Max = 580,
                                Min = -580
                            }
                        });
                }
                else
                {
                    ProcessReport(new DataBuffer()
                    {
                        Raw = (uint)Random.Shared.Next()
                    });
                }
            }
            else
            {
                while (reports.TryDequeue(out var report))
                    ProcessReport(report);
            }
#else
            while (reports.TryDequeue(out var report))
                ProcessReport(report);
#endif
            // Due to circumstances beyond my understanding, I cannot, for the life of me, fix the Peak/Valley calcs. So uh, this works

            if (sw.Elapsed > TimeSpan.FromSeconds(10))
            {
                UpdatePinConfigFile();
                sw.Restart();
            }

            Thread.Sleep(5);
        }
    }

    private void ProcessReport(DataBuffer report)
    {
        Buffer.Add(report);
        if (Buffer.Count >= SampleSize)
        {
            BufferBack = Interlocked.Exchange(ref Buffer, BufferBack);

            int peak = 0;
            for (int i = 0; i < BufferBack.Count; i++)
                peak += BufferBack[i].Voltage.Max;

            var processedReport = VoltageReport.FromArduinoZMBT101BAnalogReading(peak / ((double)BufferBack.Count));
            History.Add(processedReport);
            Latest = processedReport;
            BufferBack.Clear();
            if (IncomingNewReport is not null)
                Task.Run(() => IncomingNewReport?.Invoke(processedReport));
        }
    }

    private void UpdatePinConfigFile()
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            if (File.Exists(ConfigFile) is false)
            {
                File.WriteAllText(ConfigFile, Port.PortName = DefaultComPort);
                ConfigFileLastUpdate = File.GetLastWriteTime(ConfigFile);
            }
            else if (File.GetLastWriteTime(ConfigFile) > ConfigFileLastUpdate)
            {
#if DEBUG
                try
                {
                    OpenPort();
                    DEBUG_NoPort = false;
                }
                catch
                {
                    DEBUG_NoPort = true;
                    Log.Warning("No port has been found, running on emulated data for debug purposes.");
                }
#else
                OpenPort();
#endif
            }
        }
        catch (Exception e)
        {
            Log.Fatal(e, "An error ocurred while trying to update and open the Serial Port");
        }

        void OpenPort()
        {
            Port.Close();
            Port.PortName = File.ReadAllText(ConfigFile).Replace("\n", "").Replace("\r", "");
            ConfigFileLastUpdate = File.GetLastWriteTime(ConfigFile);
            Port.Open();
        }
    }
}

