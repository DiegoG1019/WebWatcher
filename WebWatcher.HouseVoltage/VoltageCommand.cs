using DiegoG.TelegramBot;
using DiegoG.TelegramBot.Types;
using Telegram.Bot;

namespace WebWatcher.HouseVoltage;

[BotCommand]
public class VoltageCommand : IBotCommand
{
    private class SeparatorClosure
    {
        public static readonly TimeSpan Separator = TimeSpan.FromMinutes(10);
        public const int MaxReports = 10;

        public DateTime Last;
        public int Remaining = 6;
        public int PostedReports = 0;
        public VoltageReport.ReportLabel LastLabel;

        public bool Predicate(VoltageReport report)
        {
            if (Remaining > 0 && PostedReports <= MaxReports && (report.TimeStamp - Last > Separator || report.Label != LastLabel))
            {
                Last = report.TimeStamp;
                if (report.Label != LastLabel &&
                    report.Label is not VoltageReport.ReportLabel.BelowNormal or VoltageReport.ReportLabel.AboveNormal or VoltageReport.ReportLabel.Normal)
                    Remaining--;
                PostedReports++;
                LastLabel = report.Label;
                return true;
            }

            return false;
        }
    }

    public Task<CommandResponse> Action(BotCommandArguments args)
    {
        var sb = StringBuilderStore.GetSharedStringBuilder();
        sb.Append("#VoltageReport:\n");

        foreach (var r in VoltageSubscription.Tracker.Reports.OrderByDescending(x => x.TimeStamp).Where(new SeparatorClosure().Predicate)) 
        {
            sb.Append(r.TimeStamp.ToString("ddd (dd/MM/yyyy) hh:mm:ss tt (UTCzzz)\n"))
                .Append("<b>RMS</b>: <u>").Append(r.RMS.ToString("0")).Append("</u>\n")
                .Append("<b>Peak</b>: ").Append(r.Peak.ToString("0.00")).AppendLine()
                .Append("<b>Valley</b>: ").Append(r.Valley.ToString("0.00")).AppendLine();

            sb.Append(r.Label switch
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

            sb.Append("\n\n");
        }

        string str = sb.ToString();
        if (string.IsNullOrWhiteSpace(str))
            return Task.FromResult(new CommandResponse(false));

        return Task.FromResult(new CommandResponse(false, x => x.SendTextMessageAsync(args.FromChat, str, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html)));
    }

    public Task<CommandResponse> ActionReply(BotCommandArguments args)
    {
        throw new NotImplementedException();
    }

    public void Cancel(Telegram.Bot.Types.User user)
    {
        throw new NotImplementedException();
    }

    public TelegramBotCommandClient Processor { get; set; }
    public string HelpExplanation => "Queries whether or not there's voltage in the server's location, reading as configured by the GPIO";
    public string HelpUsage => "/voltage";
    public IEnumerable<OptionDescription>? HelpOptions { get; }
    public string Trigger => "/voltage";
    public string? Alias { get; }
}
