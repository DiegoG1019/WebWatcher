using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DiegoG.Utilities.IO;
using HtmlAgilityPack;
using Serilog;

namespace DiegoG.WebWatcher
{
    public class WitchCultWatcher : IWebWatcher
    {
        private const long WitchCultChatID = -1001321283021;
        private string? LastUpload = null;

        public TimeSpan Interval { get; } = TimeSpan.FromHours(1);

        public Task Check() => Task.Run(() =>
        {
            Log.Information("Checking WitchCultTranslations for changes");
            try
            {
                Log.Debug("Getting Web Response from WitchCultTranslations");
                using StreamReader sR = new(WebRequest.Create("https://witchculttranslation.com/").GetResponse().GetResponseStream());
                var doc = new HtmlDocument();
                doc.LoadHtml(sR.ReadToEnd());
                var r = doc.DocumentNode.Descendants("ul")
                .Where(node => node.HasClass("rpwe-ul"))
                .First()
                .FirstChild
                .Descendants("h3")
                .First()
                .FirstChild;

                Log.Debug("Matching Text from WitchCultTranslations");

                var title = Regex.Replace(r.InnerText, @"[#-&]+\d+;", "").Replace("-", @"\-").Replace("#", @"\#");
                var link = r.GetAttributeValue("href", null);

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
                    Log.Information("Succesfully checked WitchCultTranslations. No updates.");
                    return;
                }

                Log.Information("New upload from WitchCultTranslations detected, notifying"); 
                OutputBot.SendTextMessage(WitchCultChatID, $"New Upload: [*{title}*]({link})", Telegram.Bot.Types.Enums.ParseMode.MarkdownV2);

                Log.Debug("Updating LastUpload information");
                LastUpload = title;
                Serialization.Serialize.Json(LastUpload, LastPostDir, LastPostFile);
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
        }
    }
}
