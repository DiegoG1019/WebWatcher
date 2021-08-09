using DiegoG.TelegramBot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

[assembly: InternalsVisibleTo("DiegoG.WebWatcher")]
namespace DiegoG.WebWatcher
{
    public static class OutBot
    {
        public static TelegramBotCommandClient Processor { get; internal set; }
        public static void EnqueueAction(MessageQueue.BotAction action) => Processor.MessageQueue.EnqueueAction(action);
        public static Task<TResult> EnqueueFunc<TResult>(MessageQueue.BotFunc<TResult> func) => Processor.MessageQueue.EnqueueFunc(func);
    }
}
