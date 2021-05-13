using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DiegoG.TelegramBot;
using DiegoG.Utilities;
using DiegoG.Utilities.IO;
using DiegoG.Utilities.Settings;
using HtmlAgilityPack;
using Serilog;

namespace DiegoG.WebWatcher
{
    //[Watcher]
    public class EraiRawsWatcher : IWebWatcher
    {
        private struct DownloadBuffer
        {
            public string Name { get; private set; }
            public int Episode { get; private set; }
            public string Link { get; private set; }
            public DownloadBuffer(string name, int episode, string link)
            {
                Name = name;
                Episode = episode;
                Link = link;
            }
        }

        private const long EraiRawsChatID = -1001267384658;

        public TimeSpan Interval { get; } = TimeSpan.FromHours(.5);

        public string Name => "EraiRawsWatcher";

        static public readonly List<Process> TorrentProcesses = new();
        public Dictionary<string, int> LatestUploads;

        public async Task Check()
        {
            AsyncTaskManager tasks = new();
            foreach (var kv in LatestUploads)
                tasks.Run(() =>
                {
                    try
                    {
                        Log.Information($"Scraping {kv.Key}");
                        using StreamReader sR = new(WebRequest.Create(kv.Key).GetResponse().GetResponseStream());
                        var doc = new HtmlDocument();
                        doc.LoadHtml(sR.ReadToEnd());

                        var menu = doc.DocumentNode.Descendants("div");
                        var m1 = menu.First(c => c.HasClass("col-12 col-sm-12 col-md-12 col-lg-12 col-xl-12 posmain h-episodes show-episodes"));
                        var m2 = m1.Descendants().Where(n => n.Name == "article").Select(c => c.FirstChild);

                        Log.Debug($"Reviewing each of the articles in {kv.Key}");

                        foreach(var n in menu)
                        {
                            var names = n.FirstChild;
                            var links = n.ChildNodes.ElementAt(2);

                            var chap = int.Parse(names.FirstChild
                            .FirstChild
                            .Descendants("font")
                            .First(c => c.HasClass("aa_ss_ops"))
                            .InnerText);

                            if (chap > kv.Value)
                            {
                                Log.Debug($"Found chapter: {chap} of {kv.Key}, downloading");
                                DownloadMagnet(new(kv.Key, chap, links.Descendants().First(c => c.InnerText == "Magnet").InnerText));
                                OutputBot.SendTextMessage(EraiRawsChatID, $"Uploaded {kv.Key} - {chap}");
                            }
                        }
                    }
                    catch(Exception e)
                    {
                        Log.Error($"Failed to check latest upload of Erai-raws: {kv.Key}. {e.GetType().Name}::{e.Message}");
                    }
                });

            AsyncTaskManager processWatcherTasks = new();

            for(int i = 0; i < TorrentProcesses.Count; i++)
            {
                TorrentProcesses[i].StandardOutput.ReadToEnd().Contains("Seeding");
                TorrentProcesses[i].Close();
                TorrentProcesses[i].TryDispose();
            }

            await tasks;
        }

        private static void DownloadMagnet(DownloadBuffer download)
        {
            //var settings = Settings<EraiRawsWatcherSettings>.Current;

            //TorrentProcesses.Add(Process.Start(settings.StartTorrentCommand
            //    .Replace("{directoryname}", settings.TorrentDirectory)
            //    .Replace("{magnetlink}", download.Link)));
        }

        public Task FirstCheck() => Check();

        readonly static string LastPostDir = Directories.InData("ER");
        readonly static string LastPostFile = "lastposts";
        public EraiRawsWatcher()
        {
            try
            {
                LatestUploads = Serialization.Deserialize<Dictionary<string, int>>.Json(LastPostDir, LastPostFile);
                return;
            }
            catch (Exception)
            {
                Directory.CreateDirectory(LastPostDir);

                LatestUploads = new()
                {
                    { "https://www.erai-raws.info/anime-list/boku-no-hero-academia-5th-season/", 4 },
                    { "https://www.erai-raws.info/anime-list/subarashiki-kono-sekai-the-animation/", 0 },
                    { "https://www.erai-raws.info/anime-list/ijiranaide-nagatoro-san/", 0 }
                };

                Serialization.Serialize.Json(LatestUploads, LastPostDir, LastPostFile);
            }

            Settings<EraiRawsWatcherSettings>.Initialize(Directories.Configuration, "erw_config.cfg");
        }
    }
}