using DiegoG.TelegramBot;
using DiegoG.Utilities;
using DiegoG.Utilities.IO;
using DiegoG.Utilities.Settings;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DiegoG.WebWatcher
{
    public static class OutputBot
    {
        //These are all assigned in Initialize. It's not a static constructor because I need to be able to control when its called
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public static TelegramBotCommandClient Client { get; private set; }

        internal static List<AdminUser> AccessList { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        
        public static event EventHandler<MessageEventArgs>? OnMessage;

        internal static void CommitAccessListToDisk()
            => Serialization.Serialize.Json(AccessList, Directories.Data, "allow");

        private static readonly object InitSync = new();
        private static bool IsInit;
        public static void Initialize()
        {
            lock (InitSync)
            {
                if (IsInit)
                    throw new InvalidOperationException("Cannot Initialize Twice");
                IsInit = true;
            }

            if (System.IO.File.Exists(Directories.InData("allow.json")))
                AccessList = (List<AdminUser>)Serialization.Deserialize.Json(Directories.Data, "allow", typeof(List<AdminUser>));
            else
            {
                Serialization.Serialize.Json(new List<AdminUser>() { new(-1, AdminRights.Disallow) }, Directories.Data, "allow");
                throw new InvalidDataException($"Could not find file: {Directories.InData("allow.json")}. This file is necessary for the bot to function.");
            }

            Client = new(Settings<WatcherSettings>.Current.BotAPIKey!, 20, messageFilter: e => AccessList.Any(u => u.User == e.From.Id && u.Rights > AdminRights.Disallow));

            Client.CommandCalled += (s,e) =>
            {
                var c = e.Arguments[0];
                var u = e.User.Id;
                var dir = Service.DaemonStatistics.TotalCommandsExecutedPerUser;

                if (!dir.ContainsKey(c))
                    dir.Add(c, new());

                if (!dir[c].ContainsKey(u))
                    dir[c].Add(u, 0);

                dir[c][u]++;
            };
        }

        internal static AdminRights GetAdmin(User user)
            => AccessList.FirstOrDefault(u => u.User == user.Id)?.Rights ?? AdminRights.Disallow;

        internal static bool GetAdmin(long user, [NotNullWhen(true)]out AdminUser? admin)
        {
            admin = AccessList.FirstOrDefault(u => u.User == user);
            return admin is not null;
        }

        internal record AdminUser(
            int User,
            AdminRights Rights
            ) { }
    }
}
