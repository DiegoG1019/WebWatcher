using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DiegoG.WebWatcher;

namespace WebWatcher.HouseVoltage;

public partial class VoltageTracker
{
    private void CompileReportsWorker()
    {
        while (true)
        {
            while (reports.TryDequeue(out var result))
            {
                var report = new VoltageReport(result.Peak, result.Valley);
                History.Add(report);
                IncomingNewReport?.Invoke(report);
            }
            Thread.Sleep(100);
        }
    }

    private void ReadReportsWorker()
    {
        Span<ushort> ReadBuffer = stackalloc ushort[2];
        while (true)
        {
            UpdatePinConfigFile();
            ReadBuffer.Clear();
            if (Reader.ReadNext(MemoryMarshal.AsBytes(ReadBuffer)) is SerialReader.Status.Success)
            {
                Debug.Assert(ReadBuffer[0] >= ReadBuffer[1], "The peak is less than the valley");
                reports.Enqueue((ReadBuffer[0], ReadBuffer[1]));
            }

            Thread.Sleep(25);
        }
    }
}
