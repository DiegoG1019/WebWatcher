using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DiegoG.TelegramBot;
using DiegoG.TelegramBot.Types;
using Telegram.Bot.Types;

namespace DiegoG.WebWatcher.BotCommands;

[BotCommand]
public class AllowCommand : IBotCommand
{
    public string HelpExplanation { get; } = "Adds a new Telegram User as a valid Bot User";

    public string HelpUsage { get; } = "/allow (userid) (rights)";

    private static string GetAdminRights()
    {
        var s = "User Rights Available: ";
        foreach (var n in Enum.GetValues(typeof(AdminRights)))
            s += $"{(int)n} : {n}, ";
        return s[..^2];
    }

    public IEnumerable<OptionDescription>? HelpOptions { get; } = new OptionDescription[]
    {
        new("userid","The numeric user id used to identify the user. You can use @raw_data_bot for this."),
        new("rights", GetAdminRights()),
    };

    public string Trigger { get; } = "/allow";

    public string? Alias => null;

    public TelegramBotCommandClient Processor { get; set; }

    public async Task<CommandResponse> Action(BotCommandArguments arguments)
    {
        var args = arguments.Arguments;
        if (!OutputBot.GetAdmin(arguments.User.Id, out var u) || u.Rights != AdminRights.Creator)
            return new(arguments.Message, false, "You do not have the rights to perform this operation");

        if (args.Length < 3)
            return new(arguments.Message, false, "Not enough arguments for the operation");

        if (!int.TryParse(args[1], out var userid))
            return new(arguments.Message, false, "Invalid UserID");

        if (Enum.TryParse<AdminRights>(args[2], out var r) && Enum.GetName(r) is not null)
        {
            if (r is AdminRights.Disallow)
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
            return new(arguments.Message, false, "Succesfully updated the AccessList");
        }

        return new(arguments.Message, false, "Invalid Admin Right");
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
