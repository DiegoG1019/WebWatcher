using DiegoG.TelegramBot.Types;
using DiegoG.WebWatcher;

namespace WebWatcher.HouseVoltage;

[Subscription]
public class VoltageSubscription : ISubscription
{
    public string Name => "VoltageSubscription";
    public string Description => "Notifies when a change regarding the server's location voltage";
    public TimeSpan Interval => TimeSpan.FromSeconds(6);

    public Task Subscribed(Telegram.Bot.Types.ChatId chat)
        => Task.CompletedTask;

    public Task Unsubscribed(Telegram.Bot.Types.ChatId chat)
        => Task.CompletedTask;

    private bool OutageReported;

    public Task Report(IEnumerable<Telegram.Bot.Types.ChatId> subscribers)
    {
        var r = VoltageReport.Latest;
        string msg;

        if (r.Label is VoltageReport.ReportLabel.Normal or VoltageReport.ReportLabel.BelowNormal or VoltageReport.ReportLabel.AboveNormal)
        {
            if (OutageReported)
            {
                msg = GenMessage(r);
                foreach (var chat in subscribers)
                    OutBot.EnqueueAction(x => x.SendTextMessageAsync(chat, msg, Telegram.Bot.Types.Enums.ParseMode.Html));

                OutageReported = false;
            }
        }
        else
        {
            msg = GenMessage(r);
            foreach (var chat in subscribers)
                OutBot.EnqueueAction(x => x.SendTextMessageAsync(chat, msg, Telegram.Bot.Types.Enums.ParseMode.Html));

            OutageReported = true;
        }

        return Task.CompletedTask;
    }

    private static string GenMessage(in VoltageReport report)
    {
        var sb = StringBuilderStore.GetSharedStringBuilder();

        sb.Append(report.TimeStamp.ToString("ddd (dd/MM/yyyy) hh:mm:ss tt (UTCzzz)\n"))
            .Append("<b>RMS</b>: <u>").Append(report.RMS.ToString("0")).Append("</u>\n")
            .Append("<b>Peak</b>: ").Append(report.Peak.ToString("0.00")).AppendLine()
            .Append("<b>Valley</b>: ").Append(report.Valley.ToString("0.00")).AppendLine();

        sb.Append(report.Label switch
        {
            VoltageReport.ReportLabel.Outage => "<u>Outage</u> ⚠️❌",
            VoltageReport.ReportLabel.BrownOut => "<u>Brown-out ⚠️⭕️",
            VoltageReport.ReportLabel.BelowNormal => "<u>Below Normal</u> ⚠️",
            VoltageReport.ReportLabel.Normal => "<u>Normal</u> ✅",
            VoltageReport.ReportLabel.PotentPowerSurge => "<u>Power Surge</u> ⚠️📛",
            VoltageReport.ReportLabel.PowerSurge => "<u>Power Surge</u> ⚠️⭕️",
            VoltageReport.ReportLabel.AboveNormal => "<u>Above Normal</u> ⚠️",
            _ => "<u>Unknown label, report to bot administrator</u>"
        });

        return sb.ToString();
    }
}
