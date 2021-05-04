using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiegoG.Utilities;
using Telegram.Bot.Types;
using DiegoG.TelegramBot.Types;

namespace DiegoG.WebWatcher.BotCommands
{
    [BotCommand]
    public class AllowCommand : IBotCommand
    {
        public string HelpExplanation { get; } = "Adds a new Telegram User as a valid Bot User";

        public string HelpUsage { get; } = "/allow (userid) (rights)";

        private static string GetAdminRights()
        {
            var s = "User Rights Available: ";
            foreach (var n in Enum.GetValues(typeof(OutputBot.AdminRights)))
                s += $"{(int)n} : {n}, ";
            return s[..^2];
        }

        public IEnumerable<(string Option, string Explanation)>? HelpOptions { get; } = new[]
        {
            ("userid","The numeric user id used to identify the user. You can use @raw_data_bot for this."),
            ("rights",GetAdminRights()),
        };

        public string Trigger { get; } = "/allow";

        public string? Alias => null;

        public Task<(string, bool)> Action(BotCommandArguments arguments)
        {
            var args = arguments.Arguments;
            if (!OutputBot.GetAdmin(arguments.User.Id, out var u) || u.Rights != OutputBot.AdminRights.Creator)
                return Task.FromResult(("You do not have the rights to perform this operation", false));

            if (args.Length < 3)
                return Task.FromResult(("Not enough arguments for the operation", false));

            if (!int.TryParse(args[1], out var userid))
                return Task.FromResult(("Invalid UserID", false));

            if(Enum.TryParse<OutputBot.AdminRights>(args[2], out var r) && Enum.GetName(r) is not null)
            {
                if (r is OutputBot.AdminRights.Disallow)
                    OutputBot.AccessList.RemoveAt(OutputBot.AccessList.FindIndex(s => s.User == userid));
                else
                {
                    var i = OutputBot.AccessList.FindIndex(d => d.User == userid);
                    if (i is not -1)
                        OutputBot.AccessList[i] = new(userid, r);
                    else
                        OutputBot.AccessList.Add(new(userid, r));
                }
                
                OutputBot.CommitAccessListToDisk();
                return Task.FromResult(("Succesfully updated the AccessList", false));
            }

            return Task.FromResult(("Invalid Admin Right", false));
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
