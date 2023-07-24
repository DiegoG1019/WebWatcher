using System.Buffers;
using System.Diagnostics;
using DiegoG.Utilities;

namespace WebWatcher.HouseVoltage;

public readonly struct VoltageReport
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

    public VoltageReport(ushort peak, ushort valley)
    {
        const double coeficient = 0.35355339059327376220042218105242; // 1 / (2√2)
        const double ArduinoADCDigitalRange = 1023;
        const double ArduinoADCRepresentedVoltage = 5;
        const double ZMPT101BVoltageOffset = 2.5;
        const double ZMPT101BVoltageCoeficient = 124.4;

        Peak = ((((peak / ArduinoADCDigitalRange) * ArduinoADCRepresentedVoltage) - ZMPT101BVoltageOffset) * ZMPT101BVoltageCoeficient);
        Valley = ((((valley / ArduinoADCDigitalRange) * ArduinoADCRepresentedVoltage) - ZMPT101BVoltageOffset) * ZMPT101BVoltageCoeficient);
        //            (The percent data represents of the range) 
        //           (          The voltage the value represents, from 0.0v to 5.0v           )
        //          (                       The voltage the ZMPT101B actually gave us from -2.5 to 2.5                     )
        //         (                                               The voltage the ZMPT101B measured from the wall                                         )

        RMS = coeficient * Peak - Valley;

        TimeStamp = DateTime.Now;

        Debug.Assert(double.IsRealNumber(RMS));
        Label = DiegoGMath.TolerantCompare(RMS, 110, 5) is 0
            ? ReportLabel.Normal
            : RMS switch
            {
                < 10 => ReportLabel.Outage,
                < 95 => ReportLabel.BrownOut,
                < 110 => ReportLabel.BelowNormal,
                110 => ReportLabel.Normal,
                > 130 => ReportLabel.PotentPowerSurge,
                > 125 => ReportLabel.PowerSurge,
                > 110 => ReportLabel.AboveNormal,
                _ => throw new InvalidProgramException()
            };
    }

    public static VoltageReport CreateFromSet(ReadOnlySpan<ushort> data)
    {
        ushort peak = 0;
        ushort valley = ushort.MaxValue;
        for (int i = 0; i < data.Length; i++)
        {
            peak = ushort.Max(peak, data[i]);
            valley = ushort.Min(valley, data[i]);
        }

        return new VoltageReport(peak, valley);
    }

    public ReportLabel Label { get; }
    public DateTime TimeStamp { get; }
    public double Peak { get; }
    public double Valley { get; }
    public double RMS { get; }
}
