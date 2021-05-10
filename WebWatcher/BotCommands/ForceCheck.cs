using DiegoG.TelegramBot.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace DiegoG.WebWatcher.BotCommands
{
    [BotCommand]
    public class ForceCheck : IBotCommand
    {
        public string HelpExplanation => "Forces a Watch routine";

        public string HelpUsage => "/forcecheck (WatcherName)";

        public IEnumerable<(string Option, string Explanation)>? HelpOptions => null;

        public string Trigger => "/forcecheck";

        public string? Alias => null;

        public Task<(string Result, bool Hold)> Action(BotCommandArguments args)
        {
            if (args.Arguments.Length < 2)
                return Task.FromResult(("Too few arguments.", false));
            if (args.Arguments.Length > 2)
                return Task.FromResult(("Too many arguments.", false));

            try
            {
                Service.ForceCheck(args.Arguments[1]);
                return Task.FromResult(($"Succesfully forced routine {args.Arguments[1]} to run", false));
            }
            catch (ArgumentException e)
            {
                return Task.FromResult((e.Message, false));
            }
        }

        public Task<(string Result, bool Hold)> ActionReply(BotCommandArguments args)
        {
            throw new NotImplementedException();
        }

        public void Cancel(User user)
        {
            throw new NotImplementedException();
        }
    }
}
