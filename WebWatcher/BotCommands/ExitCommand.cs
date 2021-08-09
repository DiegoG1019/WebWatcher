using DiegoG.TelegramBot.Types;
using DiegoG.TelegramBot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using DiegoG.Utilities.Settings;

namespace DiegoG.WebWatcher.BotCommands
{
    [BotCommand]
    public class ExitCommand : IBotCommand
    {
        public string HelpExplanation { get; } = "Requests the bot server to stop the bot";

        public string HelpUsage { get; } = "/exit";

        public IEnumerable<OptionDescription>? HelpOptions => null;

        public string Trigger => "/exit";

        public string? Alias => null;

        public TelegramBotCommandClient Processor { get; set; }

        private List<User> Held { get; } = new();
        public IEnumerable<User>? Hold => Held;

        public void Cancel(User user)
        {
            if (Held.Contains(user))
                Held.Remove(user);
        }

        public async Task<CommandResponse> ActionReply(BotCommandArguments args)
        {
            if (Held.Contains(args.User))
            {
                if (args.Arguments[0] == "yes")
                {
                    _ = Task.Run(async () =>
                    {
						await Task.WhenAll(new[]{ Settings<WatcherSettings>.SaveSettingsAsync(), Task.Delay(1000) });
                        Environment.Exit(0);
                    });
                    Cancel(args.User);
                    return new(args.Message, false, "Bot shutting down in 1000ms");
                }
                Cancel(args.User);
                return new(args.Message, false, "Cancelling Exit order");
            }
            return new(args.Message, false, "Exit order automatically canceled");
        }

        public async Task<CommandResponse> Action(BotCommandArguments args)
        {
            if (!OutputBot.GetAdmin(args.User.Id, out var adm) || adm.Rights < AdminRights.Admin)
                return new(args.Message, false, "You do not have permissions to perform this operation");

            Held.Add(args.User);
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(1));
                Cancel(args.User);
            });
            return new(args.Message, true, "Are you sure you want the bot to exit? Please write 'yes' if so.");
        }
    }
}
