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
        public static TelegramBotClient Client { get; private set; }
        public static User BotMe { get; private set; }
        
        private static Thread BotMessageThread { get; set; }
        private readonly static ConcurrentQueue<BufferedMessage> MessageQueue = new();

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

            AccessList = System.IO.File.Exists(Directories.InData("allow.json"))
                ? (List<AdminUser>)Serialization.Deserialize.Json(Directories.Data, "allow", typeof(List<AdminUser>))
                : throw new InvalidDataException($"Could not find file: {Directories.InData("allow.json")}. This file is necessary for the bot to function.");

            Client = new(Settings<WatcherSettings>.Current.BotAPIKey);
            BotMessageThread = new(async () =>
            {
                AsyncTaskManager tasks = new();
#if DEBUG
                ;
#endif
                while (true)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    for(int i = 0; i < 20 && !MessageQueue.IsEmpty; i++)
                    {
                        if(MessageQueue.TryDequeue(out var msg))
                            tasks.Run(async () =>
                            {
                                try
                                {
                                    await Client.SendTextMessageAsync(msg.ChatId, msg.Text, msg.ParseMode, msg.DisableWebPreview, msg.DisableNotification, msg.ReplyToMessageId, msg.ReplyMarkup);
                                }
                                catch (Exception)
                                {
                                    MessageQueue.Enqueue(msg);
                                    await Task.Delay(TimeSpan.FromMinutes(1));
                                }
                            });
                    }
                    await tasks;
                }

            });

            Client.Timeout = TimeSpan.FromSeconds(30);
            Client.OnMessage += Client_OnMessage;

            BotMessageThread.Start();

            BotCommandProcessor.CommandCalled += (s,e) =>
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

        public static void StartReceiving(UpdateType[] updateTypes) => Client.StartReceiving(updateTypes);
        public static void StopReceiving()
        {
            if(Client is not null && Client.IsReceiving)
                Client.StopReceiving();
        }

        internal static bool GetAdmin(int user, [NotNullWhen(true)]out AdminUser? admin)
        {
            admin = AccessList.FirstOrDefault(u => u.User == user);
            return admin is not null;
        }

        private static void Client_OnMessage(object? sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            var user = e.Message.From;
            Log.Information($"New Message from user {user}, chatId: {e.Message.Chat.Id}");
            Log.Debug($"{user} Sent: {e.Message.Text}");
            if (!AccessList.Any(u=>u.User == user.Id))
            {
                Log.Information($"User {user} not found in AccessList, ignoring");
                return;
            }
            
            Log.Debug($"Forwarding message from {user} to subscribers");
            OnMessage?.Invoke(sender, e);
        }

        public static void SendTextMessage(
            ChatId chatId,
            string text,
            ParseMode parseMode = ParseMode.Default,
            bool disableWebPreview = false,
            bool disableNotification = false,
            int replyToMessageId = 0,
            Telegram.Bot.Types.ReplyMarkups.IReplyMarkup? replyMarkup = null)
        {
            MessageQueue.Enqueue(new(chatId, text, parseMode, disableWebPreview, disableNotification, replyToMessageId, replyMarkup));
        }

        private record BufferedMessage(
            ChatId ChatId, 
            string Text,
            Telegram.Bot.Types.Enums.ParseMode ParseMode = Telegram.Bot.Types.Enums.ParseMode.Default,
            bool DisableWebPreview = false,
            bool DisableNotification = false,
            int ReplyToMessageId = 0,
            Telegram.Bot.Types.ReplyMarkups.IReplyMarkup? ReplyMarkup = null
            ){ }

        internal enum AdminRights
        {
            Disallow,
            User,
            Moderator,
            Admin,
            Creator
        }
        internal record AdminUser(
            int User,
            AdminRights Rights
            ) { }
    }
}
