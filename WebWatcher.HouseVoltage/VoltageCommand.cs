using DiegoG.TelegramBot;
using DiegoG.TelegramBot.Types;
using Microsoft.Extensions.Primitives;

namespace WebWatcher.HouseVoltage;

[BotCommand]
public class VoltageCommand : IBotCommand
{
    public Task<CommandResponse> Action(BotCommandArguments args)
    {
        var sb = StringBuilderStore.GetSharedStringBuilder();
        sb.Append("#VoltageReport:\n");

        int i = 6;
        foreach (var r in VoltageReport.Reports)
        {
            if (i-- <= 0) break;
            sb.Append(r.TimeStamp.ToString("ddd (dd/MM/yyyy) hh:mm:ss tt (UTCzzz)\n"))
                .Append("<b>RMS</b>: <u>").Append(r.RMS.ToString("0")).Append("</u>\n")
                .Append("<b>Peak</b>: ").Append(r.Peak.ToString("0.00")).AppendLine()
                .Append("<b>Valley</b>: ").Append(r.Valley.ToString("0.00")).AppendLine();

            sb.Append(r.Label switch
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

            sb.Append("\n\n");
        }

        return Task.FromResult(new CommandResponse(false, x => x.SendTextMessageAsync(args.FromChat, sb.ToString(), Telegram.Bot.Types.Enums.ParseMode.Html)));
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
