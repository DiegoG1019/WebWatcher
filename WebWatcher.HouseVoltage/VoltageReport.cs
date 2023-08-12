using System.Diagnostics;

namespace WebWatcher.HouseVoltage;

public readonly struct VoltageReport
{
    private const double SquareRoot2 = 1.4142135623730950488016887242097;
    private const double RMSPeakCoeficient = 0.70710678118654752440084436210485; // 1 / (√2)
    private const double RMSPeakValleyCoeficient = 0.35355339059327376220042218105242; // 1 / (2√2)
    private const double RMSWaveAverageCoeficient = 1.1107207345395915617539702475152; // PI / (2√2)

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

    public enum ReportKind
    {
        PeakValleyRMS,
        PeakRMS,
        WaveAverageRMS
    }

    public VoltageReport(double peak)
    {
        Peak = peak;
        Valley = -Peak;
        RMS = RMSPeakCoeficient * Peak;

        TimeStamp = DateTime.Now;

        Debug.Assert(double.IsRealNumber(RMS));
        Label = GetLabel(RMS);
        Kind = ReportKind.PeakRMS;
    }

    public VoltageReport(double peak, double valley)
    {
        Peak = peak;
        Valley = valley;
        RMS = RMSPeakValleyCoeficient * Peak - Valley;

        TimeStamp = DateTime.Now;

        Debug.Assert(double.IsRealNumber(RMS));
        Label = GetLabel(RMS);
        Kind = ReportKind.PeakValleyRMS;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// <c>"Vavg: The level of a waveform defined by the condition that the area enclosed by the curve above this level is exactly equal to the area enclosed by the curve below this level."</c> Ensure that waveSamples have as many, and the same length of the positive peak as the negative peak of the measured waveform
    /// </remarks>
    /// <param name="waveSamples"></param>
    public VoltageReport(ReadOnlySpan<double> waveSamples)
    {
        Peak = double.MinValue;
        Valley = double.MaxValue;
        double acc = 0;

        for (int i = 0; i < waveSamples.Length; i++)
        {
            double sample = waveSamples[i];
            Peak = double.Max(Peak, sample);
            Valley = double.Min(Valley, sample);
            acc += sample;
        }

        RMS = RMSWaveAverageCoeficient * acc / waveSamples.Length;

        TimeStamp = DateTime.Now;

        Debug.Assert(double.IsRealNumber(RMS));
        Label = GetLabel(RMS);
        Kind = ReportKind.WaveAverageRMS;
    }

    public static VoltageReport FromArduinoZMBT101BAnalogReading(double peak)
        => new(((((peak / SquareRoot2) - 420.76) / -90.24) * -210.2) + 210.2);

    public static VoltageReport FromArduinoZMBT101BAnalogReading(double peak, double valley)
    {
        var calcp = ((((peak / SquareRoot2) - 420.76) / -90.24) * -210.2) + 210.2;
        var calcv = ((((double.Abs(valley) / SquareRoot2) - 420.76) / -90.24) * -210.2) + 210.2;
        return new VoltageReport(calcp, double.CopySign(calcv, valley));
    }

    private static ReportLabel GetLabel(double rms)
        => rms switch
        {
            < 30 => ReportLabel.Outage,
            < 85 => ReportLabel.BrownOut,
            < 105 => ReportLabel.BelowNormal,
            > 165 => ReportLabel.PotentPowerSurge,
            > 150 => ReportLabel.PowerSurge,
            > 140 => ReportLabel.AboveNormal,
            >= 105 and <= 140 => ReportLabel.Normal,
            _ => throw new InvalidProgramException()
        };

    public ReportKind Kind { get; }
    public ReportLabel Label { get; }
    public DateTime TimeStamp { get; }
    public double Peak { get; }
    public double Valley { get; }
    public double RMS { get; }
}
