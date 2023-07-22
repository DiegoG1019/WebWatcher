using DiegoG.TelegramBot;
using DiegoG.TelegramBot.Types;
using Telegram.Bot.Types;

namespace WebWatcher.CommandExtensions;

[BotCommand]
public class SubscribeCommand : IBotCommand
{
    public Task<CommandResponse> Action(BotCommandArguments args)
    {
        throw new NotImplementedException();
    }

    public Task<CommandResponse> ActionReply(BotCommandArguments args)
    {
        throw new NotImplementedException();
    }

    public void Cancel(User user)
    {
        throw new NotImplementedException();
    }

    public TelegramBotCommandClient Processor { get; set; }
    public string HelpExplanation { get; }
    public string HelpUsage { get; }
    public IEnumerable<OptionDescription>? HelpOptions { get; }
    public string Trigger { get; }
    public string? Alias { get; }
}
