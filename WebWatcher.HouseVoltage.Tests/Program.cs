namespace WebWatcher.HouseVoltage.Tests;

internal class Program
{
    static void Main(string[] args)
    {
        VoltageTracker.Tracker.IncomingNewReport += Tracker_IncomingNewReport;
        while (true) Thread.Sleep(10);
    }

    private static void Tracker_IncomingNewReport(VoltageReport report)
    {
        Console.Clear();
        var writer = ConsoleWriter.Writer;

        writer.Write(report.TimeStamp.ToString("ddd (dd/MM/yyyy) hh:mm:ss tt (UTCzzz)\n"), ConsoleColor.DarkCyan)
            .Write("RMS: ", ConsoleColor.Blue).Write(report.RMS.ToString("0"), ConsoleColor.Cyan).WriteLine()
            .Write("Peak: ", ConsoleColor.Blue).Write(report.Peak.ToString("0.00"), ConsoleColor.Cyan).WriteLine()
            .Write("Valley: ", ConsoleColor.Blue).Write(report.Valley.ToString("0.00"), ConsoleColor.Cyan).WriteLine();

        var (labelmsg, labelcolor) = report.Label switch
        {
            VoltageReport.ReportLabel.Outage => ("Outage ⚠️❌", ConsoleColor.DarkRed),
            VoltageReport.ReportLabel.BrownOut => ("Brown-out ⚠️⭕️", ConsoleColor.Red),
            VoltageReport.ReportLabel.BelowNormal => ("Below Normal ⚠️", ConsoleColor.Magenta),
            VoltageReport.ReportLabel.Normal => ("Normal ✅", ConsoleColor.DarkCyan),
            VoltageReport.ReportLabel.PotentPowerSurge => ("Power Surge ⚠️📛", ConsoleColor.DarkRed),
            VoltageReport.ReportLabel.PowerSurge => ("Power Surge ⚠️⭕️", ConsoleColor.Red),
            VoltageReport.ReportLabel.AboveNormal => ("Above Normal ⚠️", ConsoleColor.Magenta),
            _ => ("Unknown label, report to bot administrator", ConsoleColor.Magenta)
        };

        writer.Write(labelmsg, labelcolor);
    }

    private class ConsoleWriter
    {
        private ConsoleWriter() { }

        public static ConsoleWriter Writer { get; } = new();

        public ConsoleWriter Write(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(message);
            return this;
        }

        public ConsoleWriter WriteLine()
        {
            Console.WriteLine();
            return this;
        }

        public ConsoleWriter WriteLine(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            return this;
        }

        public ConsoleWriter Write(string message)
        {
            Console.Write(message);
            return this;
        }

        public ConsoleWriter WriteLine(string message)
        {
            Console.WriteLine(message);
            return this;
        }
    }
}

