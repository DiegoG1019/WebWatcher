using System.Collections.Concurrent;
using System.Text;
using DiegoG.WebWatcher;
using Telegram.Bot;

namespace WebWatcher.HouseVoltage;

[Subscription]
public class VoltageSubscription : ISubscription
{
    public static VoltageTracker Tracker { get; } = new();

    public VoltageSubscription()
    {
        Tracker.IncomingNewReport += Tracker_IncomingNewReport;
    }

    private bool OutageReported;
    private readonly ConcurrentQueue<VoltageReport> Reports = new();
    private void Tracker_IncomingNewReport(VoltageReport report)
    {
        lock (Reports)
        {
            if (report.Label is VoltageReport.ReportLabel.Normal or VoltageReport.ReportLabel.BelowNormal or VoltageReport.ReportLabel.AboveNormal)
            {
                if (OutageReported)
                {
                    Reports.Enqueue(report);
                    OutageReported = false;
                }
            }
            else
            {
                Reports.Enqueue(report);
                OutageReported = true;
            }
        }
    }

    public string Name => "VoltageSubscription";
    public string Description => "Notifies when a change regarding the server's location voltage";
    public TimeSpan Interval => TimeSpan.FromSeconds(2);

    public Task Subscribed(Telegram.Bot.Types.ChatId chat)
        => Task.CompletedTask;

    public Task Unsubscribed(Telegram.Bot.Types.ChatId chat)
        => Task.CompletedTask;

    public Task Report(IEnumerable<Telegram.Bot.Types.ChatId> subscribers)
    {
        var sb = StringBuilderStore.GetSharedStringBuilder();
        while (Reports.TryDequeue(out var rep))
            GenMessage(sb, rep).Append("\n\n");

        if (sb.Length > 0)
        {
            var msg = sb.ToString();
            foreach (var chat in subscribers)
                OutBot.EnqueueAction(x => x.SendTextMessageAsync(chat, msg, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html));
        }

        return Task.CompletedTask;
    }

    public static StringBuilder GenMessage(StringBuilder builder, in VoltageReport report)
    {
        builder.Append(report.TimeStamp.ToString("ddd (dd/MM/yyyy) hh:mm:ss tt (UTCzzz)\n"))
            .Append("<b>RMS</b>: <u>").Append(report.RMS.ToString("0")).Append("</u>\n")
            .Append("<b>Peak</b>: ").Append(report.Peak.ToString("0.00")).AppendLine()
            .Append("<b>Valley</b>: ").Append(report.Valley.ToString("0.00")).AppendLine();

        builder.Append(report.Label switch
        {
            VoltageReport.ReportLabel.Outage => "<u>Outage</u> ⚠️❌",
            VoltageReport.ReportLabel.BrownOut => "<u>Brown-out</u> ⚠️⭕️",
            VoltageReport.ReportLabel.BelowNormal => "<u>Below Normal</u> ⚠️",
            VoltageReport.ReportLabel.Normal => "<u>Normal</u> ✅",
            VoltageReport.ReportLabel.PotentPowerSurge => "<u>Power Surge</u> ⚠️📛",
            VoltageReport.ReportLabel.PowerSurge => "<u>Power Surge</u> ⚠️⭕️",
            VoltageReport.ReportLabel.AboveNormal => "<u>Above Normal</u> ⚠️",
            _ => "<u>Unknown label, report to bot administrator</u>"
        });

        return builder;
    }

    public static string GenMessage(in VoltageReport report)
        => GenMessage(StringBuilderStore.GetSharedStringBuilder(), report).ToString();
}
