using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DiegoG.TelegramBot;
using DiegoG.Utilities.IO;
using HtmlAgilityPack;
using Serilog;

namespace DiegoG.WebWatcher
{
    [Watcher]
    public class WitchCultWatcher : IWebWatcher
    {
        private const long WitchCultChatID = -1001321283021;
        private string? LastUpload = null;

        public TimeSpan Interval { get; } = TimeSpan.FromHours(1);

        public string Name => "WitchCultWatcher";

        public Task Check() => Task.Run(() =>
        {
            Log.Information("Checking WitchCultTranslations for changes");
            try
            {
                Log.Debug("Getting Web Response from WitchCultTranslations");
                using StreamReader sR = new(WebRequest.Create("https://witchculttranslation.com/").GetResponse().GetResponseStream());
                var doc = new HtmlDocument();
                doc.LoadHtml(sR.ReadToEnd());

                var sidebar = doc.DocumentNode.Descendants("ul")
                .Where(node => node.HasClass("rpwe-ul"))
                .First();

                string title, link;
                string? latest = null;
                Stack<string> Stack = new();

                for (int i = 0; ; i++) 
                {
                    var r = sidebar
                    .ChildNodes[i]
                    .Descendants("h3")
                    .First()
                    .FirstChild;

                    Log.Debug("Matching Text from WitchCultTranslations");

                    title = Regex.Replace(r.InnerText, @"[#-&]+\d+;", "");
                    link = r.GetAttributeValue("href", null);

                    if (title is null)
                    {
                        Log.Warning("Could not locate The latest post from WitchCultTranslation's title");
                        throw new InvalidDataException("Could not locate The latest post from WitchCultTranslation's title");
                    }
                    if (link is null)
                    {
                        Log.Warning("Could not locate The latest post from WitchCultTranslation's link");
                        throw new InvalidDataException("Could not locate The latest post from WitchCultTranslation's link");
                    }


                    if (title == LastUpload)
                    {
                        Log.Information("Succesfully checked WitchCultTranslations.");
                        break;
                    }

                    if (i == 0)
                        latest = title;

                    Log.Information("New upload from WitchCultTranslations detected, notifying");
                    Stack.Push($"New Upload: [*{title}*]({link})");
                }

                if(latest is not null)
                {
                    Log.Debug("Updating LastUpload information");
                    LastUpload = latest;
                    Serialization.Serialize.Json(LastUpload, LastPostDir, LastPostFile);
                }

                while(Stack.Count > 0)
                    OutputBot.SendTextMessage(WitchCultChatID, Stack.Pop(), Telegram.Bot.Types.Enums.ParseMode.MarkdownV2);
            }
            catch (WebException)
            {
                Log.Error("Couldn't Connect to WitchCultTranslations. Aborting Task and trying again later");
            }
        });

        public Task FirstCheck() => Check();

        readonly static string LastPostDir = Directories.InData("WCT");
        readonly static string LastPostFile = "lastpost";
        public WitchCultWatcher()
        {
            try
            {
                LastUpload = Serialization.Deserialize<string>.Json(LastPostDir, LastPostFile);
                return;
            }
            catch (Exception) { }
            Directory.CreateDirectory(LastPostDir);
            Serialization.Serialize.Json("Arc 7, Chapter 11 \u2013 Lifeblood Ritual", LastPostDir, LastPostFile);
            LastUpload = "Arc 7, Chapter 11 \u2013 Lifeblood Ritual";
        }
    }
}