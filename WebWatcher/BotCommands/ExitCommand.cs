using DiegoG.TelegramBot.Types;
using DiegoG.TelegramBot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace DiegoG.WebWatcher.BotCommands
{
    [BotCommand]
    public class ExitCommand : IBotCommand
    {
        public string HelpExplanation { get; } = "Requests the bot server to stop the bot";

        public string HelpUsage { get; } = "/exit";

        public IEnumerable<(string Option, string Explanation)>? HelpOptions => null;

        public string Trigger => "/exit";

        public string? Alias => null;

        private List<User> Held { get; } = new();
        public IEnumerable<User>? Hold => Held;

        public void Cancel(User user)
        {
            if (Held.Contains(user))
                Held.Remove(user);
        }

        public Task<(string, bool)> ActionReply(BotCommandArguments args)
        {
            if (Held.Contains(args.User))
            {
                if (args.Arguments[0] == "yes")
                {
                    Task.Run(async () =>
                    {
                        await Task.Delay(500);
                        Environment.Exit(0);
                    });
                    Cancel(args.User);
                    return Task.FromResult(("Bot shutting down in 500ms", false));
                }
                Cancel(args.User);
                return Task.FromResult(("Cancelling Exit order", false));
            }
            return Task.FromResult(("Exit order automatically canceled", false));
        }

        public Task<(string, bool)> Action(BotCommandArguments args)
        {
            if (!OutputBot.GetAdmin(args.User.Id, out var adm) || adm.Rights < OutputBot.AdminRights.Admin)
                return Task.FromResult(("You do not have permissions to perform this operation", false));

            Held.Add(args.User);
            Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(1));
                Cancel(args.User);
            });
            return Task.FromResult(("Are you sure you want the bot to exit? Please write 'yes' if so.", true));
        }
    }
}
