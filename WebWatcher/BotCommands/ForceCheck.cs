using DiegoG.TelegramBot;
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

        public IEnumerable<OptionDescription>? HelpOptions => null;

        public string Trigger => "/forcecheck";

        public string? Alias => null;

        public TelegramBotCommandClient Processor { get; set; }

        public Task<CommandResponse> Action(BotCommandArguments args)
        {
            if (args.Arguments.Length < 2)
                return Task.FromResult(new CommandResponse(args.Message, false, "Too few arguments."));
            if (args.Arguments.Length > 2)
                return Task.FromResult(new CommandResponse(args.Message, false, "Too many arguments."));

            try
            {
                WatcherService.ForceCheck(args.Arguments[1]);
                return Task.FromResult(new CommandResponse(args.Message, false, $"Succesfully forced routine {args.Arguments[1]} to run"));
            }
            catch (ArgumentException e)
            {
                return Task.FromResult(new CommandResponse(args.Message, false, e.Message));
            }
        }

        public Task<CommandResponse> ActionReply(BotCommandArguments args)
        {
            throw new NotImplementedException();
        }

        public void Cancel(User user)
        {
            throw new NotImplementedException();
        }
    }
}
