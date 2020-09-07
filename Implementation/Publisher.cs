using Markdig;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Web.CodeGeneration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace FusekiC
{
    /// <summary>
    /// Configurations:
    /// 1. main site pulls everything into /temp
    /// 2. publishes this by rsync to target.
    /// </summary>
    public class Publisher
    {
        private Renderer Renderer { get; set; }
        
        private ConsoleLogger Logger { get; set; }
        private ArticleData ArticleData { get; set; }
        private Settings Settings { get; set; }
        private PublishConfiguration PublishConfiguration { get; set; }

        public Publisher(Renderer renderer, ConsoleLogger logger, Settings settings, PublishConfiguration pc)
        {
            ArticleData = new ArticleData();
            Renderer = renderer;
            Logger = logger;
            Settings = settings;
            PublishConfiguration = pc;
        }

        public PublishResultModel Publish()
        {
            SetupDirectories(PublishConfiguration);

            CopyOtherFiles(PublishConfiguration);

            PublishArticlesAndTags(PublishConfiguration, out var count);

            CopyToLive(PublishConfiguration, out string result);

            var pubres = new PublishResultModel();
            pubres.Details = result;
            pubres.Message = "Success";
            pubres.Count = count;

            return pubres;
        }

        

        private static bool SetupDirectories(PublishConfiguration pc)
        {
            //create empty target dir
            if (System.IO.Directory.Exists(pc.TempBase))
            {
                System.IO.Directory.Delete(pc.TempBase, true);
            }

            System.IO.Directory.CreateDirectory(pc.TempBase);
            return true;
        }

        private bool CopyOtherFiles(PublishConfiguration pc)
        {
            Helpers.DirectoryCopy(pc.CssSource, pc.TempBase + "/css", true);
            Helpers.DirectoryCopy(pc.JsSource, pc.TempBase + "/js", true);
            var imgdest = pc.TempBase + "/images";
            Logger.LogMessage($"Current location: {System.IO.Directory.GetCurrentDirectory()}");
            if (!System.IO.Directory.Exists(imgdest))
            {
                System.IO.Directory.CreateDirectory(imgdest);
            }
            Logger.LogMessage($"Copying images from:{pc.ImageSource} to {imgdest}");
            Helpers.DirectoryCopy(pc.ImageSource, imgdest, true);
            return true;
        }

  

        private bool PublishArticlesAndTags(PublishConfiguration pc, out int count)
        {
            count = 0;
            using (var db = new FusekiContext())
            {
                var articleIds = new List<int>();
                try
                {
                    foreach (var article in db.Articles
                        .Include(el => el.Tags)
                        .Where(el => el.Published == true))
                    {
                        var filename = article.MakeFilename(false);
                        Console.WriteLine($"Writing: {filename}");
                        var path = MakePath(pc, filename);
                        //todo add in better title generation code here.
                        
                        var combined = Renderer.GenerateArticleString(Settings, article, false);
                        System.IO.File.WriteAllText(path, combined);
                        articleIds.Add(article.Id);
                        Logger.LogMessage($"Published: {article.Title}");
                        count++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                

                //todo only publish tags where the article is published.
                var tags = db.Tags.Where(t => articleIds.Contains(t.ArticleId)).ToHashSet();

                var tagDir = pc.TempBase + "/tags";
                if (!System.IO.Directory.Exists(tagDir))
                {
                    System.IO.Directory.CreateDirectory(tagDir);
                }

                foreach (var tag in tags)
                {
                    PublishTag(pc, tag, false);
                }

                RecreateHtaccess(pc);

            }

            return true;
        }

        /// <summary>
        /// published articles with id<=294 will be have redirects for their titles.
        /// This doesn't cover the case an article title gets changed - those inbound links are just lost.
        /// </summary>
        private void RecreateHtaccess(PublishConfiguration pc)
        {
            var baseHtaccess = "RewriteEngine On\n" +
            "Redirect permanent \"/home/comparison.html\" \"/home/ComparisonoflifeinPiscatawayNewJerseyKochiJapanandZhuzhouChina.html\"";

            var oldIds = new List<int>() { 1, 9, 10, 11, 12, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 35, 36, 37, 38, 39, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 61, 62, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 92, 93, 94, 95, 97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 124, 125, 126, 127, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 163, 165, 178, 179, 222, 223, 224, 226, 227, 229, 234, 235, 236, 237, 244, 246, 247, 249, 250, 260, 263, 265, 267, 268, 270, 272, 273, 275, 277, 279, 282, 288, 291, 292, 293 };

            var lines = new List<string>();
            
            using (var db = new FusekiContext())
            {
                foreach (var article in db.Articles.Where(el => oldIds.Contains(el.Id) && el.Title != null && el.Title.Length>0))
                {
                    var oldFilename = article.MakeOldFilename(false);
                    var newFilename = article.MakeFilename(false);
                    if (oldFilename != newFilename) { //doh
                        var str = $"Redirect permanent \"/home/{oldFilename}\" \"/home/{newFilename}\"";
                        lines.Add(str);
                    }
                }
            }

            baseHtaccess += "\n" + string.Join("\n", lines);
            var htaccessPath = pc.TempBase + "/.htaccess";
            System.IO.File.WriteAllText(htaccessPath, baseHtaccess);

        }

        private void PublishTag(PublishConfiguration pc, Tag tag, bool inAdmin)
        {
            var filename = $"tags/{tag.MakeFilename(inAdmin)}";
            var path = MakePath(pc, filename);

            var tagString = Renderer.GetTagString(Settings, tag, inAdmin);
            File.WriteAllText(path, tagString);
        }

        private static string MakePath(PublishConfiguration pc, string fn)
        {
            return pc.TempBase + "/" + fn;
        }

        private bool CopyToLive(PublishConfiguration pc, out string result)
        {
            //figure out what files to delete from target
            //copy all the files in pc.Base to target (articles, tags, etc.)

            var ee = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            if (ee == "Development")
            {
                result = "do nothing";
            }
            else
            {
                var usingTempBase = pc.TempBase;
                if (!usingTempBase.EndsWith("/"))
                {
                    usingTempBase += '/';
                }
                var cmd = $"rsync -av --delete {usingTempBase} {pc.PublishTarget}";
                Logger.LogMessage($"Cmd: {cmd}");
                var process = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = $"-c \"{cmd}\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };
                process.Start();
                result = process.StandardOutput.ReadToEnd();
                Logger.LogMessage("Result:" + result);
                process.Dispose();
            }

            return true;
        }
 
    }
}
