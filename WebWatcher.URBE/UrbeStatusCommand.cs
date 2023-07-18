using DiegoG.TelegramBot;
using DiegoG.TelegramBot.Types;
using Telegram.Bot.Types;

namespace WebWatcher.URBE;

[BotCommand]
public class UrbeStatusCommand : IBotCommand
{
    public Task<CommandResponse> Action(BotCommandArguments args)
        => Task.FromResult(new CommandResponse(args, false, UrbeWatcher.IsActive ? $"URBE's inscription page is currently active, see at {UrbeWatcher.UrbeUri}" : $"URBE's inscription page is down, not active, see at {UrbeWatcher.UrbeUri}"));

    public Task<CommandResponse> ActionReply(BotCommandArguments args)
    {
        throw new NotImplementedException();
    }

    public void Cancel(User user)
    {
        throw new NotImplementedException();
    }

    public TelegramBotCommandClient Processor { get; set; }
    public string HelpExplanation => "Checks the current state of URBE's Inscription Page";
    public string HelpUsage => "/urbe";
    public IEnumerable<OptionDescription>? HelpOptions { get; }
    public string Trigger => "/urbe";
    public string? Alias => null;
}
