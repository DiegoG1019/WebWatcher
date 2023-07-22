using DiegoG.Utilities;

namespace WebWatcher.HouseVoltage;

public struct VoltageReport
{
    public enum ReportLabel
    {
        Normal,
        BelowNormal,
        BrownOut,
        Outage,
        AboveNormal,
        PowerSurge,
        PotentPowerSurge
    }

    private VoltageReport(VoltageReading reading)
    {
        const decimal coeficient = 0.35355339059327376220042218105242M; // 1 / (2*Sqrt(2))

        Peak = reading.Peak;
        Valley = reading.Valley;
        RMS = coeficient * Peak - Valley;
        TimeStamp = DateTime.Now;

        Label = DiegoGMath.TolerantCompare(RMS, 110, 5) is 0
            ? ReportLabel.Normal
            : RMS switch
            {
                < 10  => ReportLabel.Outage,
                < 95  => ReportLabel.BrownOut,
                < 110 => ReportLabel.BelowNormal,
                  110 => ReportLabel.Normal,
                > 130 => ReportLabel.PotentPowerSurge,
                > 125 => ReportLabel.PowerSurge,
                > 110 => ReportLabel.AboveNormal
            };
    }

    public static VoltageReport Latest { get; private set; }
    private static CircularList<VoltageReport> History { get; } = new CircularList<VoltageReport>(60);
    public static IReadOnlyCollection<VoltageReport> Reports => History;

    public ReportLabel Label { get; }
    public DateTime TimeStamp { get; }
    public short Peak { get; }
    public short Valley { get; }
    public decimal RMS { get; }

    public static VoltageReport AddReport(VoltageReading reading)
    {
        var report = new VoltageReport(reading);
        History.Add(report);
        return report;
    }
}
