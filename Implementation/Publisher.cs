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
        private static Regex AlphaNumeric = new Regex("^[a-zA-Z0-9]*$");
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

        public static string LinkToArticle(Article article, bool inTagDir, bool inAdmin = false, double distance = 0)
        {
            if (inAdmin)
            {
                return $"<a class=articlelink href=\"/{article.Title}\">{article.Title} ({distance})</a>";
            }
            else
            {
                var stem = inTagDir ? "../" : "";
                var fn = MakeFilename(article.Title, true);
                return $"<a class=articlelink href=\"{stem}{fn}\">{article.Title} ({distance})</a>";
            }


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
                foreach (var article in db.Articles
                    .Include(el => el.Tags)
                    .Where(el => el.Published == true))
                {
                    PublishArticle(pc, article);
                    articleIds.Add(article.Id);
                    Logger.LogMessage($"Published: {article.Title}");
                    count++;
                }

                //todo only publish tags where the article is published.
                var tags = db.Tags.Where(t => articleIds.Contains(t.ArticleId)).Select(el => el.Name).ToHashSet();

                var tagDir = pc.TempBase + "/tags";
                if (!System.IO.Directory.Exists(tagDir))
                {
                    System.IO.Directory.CreateDirectory(tagDir);
                }

                foreach (var tag in tags)
                {
                    PublishTag(pc, tag);
                }
            }

            return true;
        }

        private string MakeHeader()
        {
            return $"<h1><a href='/'>{Settings.SiteName}</a>";
        }

        private void PublishArticle(PublishConfiguration pc, Article article)
        {
            var header = MakeHeader();
            var title = MakeTitle(article.Title);
            var body = Renderer.ToHtml(article.Body, false);
            var footer = MakeFooter(article);
            var filename = MakeFilename(article.Title);
            var path = MakePath(pc, filename);
            var parts = new List<string>() { header, title, body, footer };
            var combined = WrapInner(parts, false, title: article.Title, mainDivClass:"article");

            System.IO.File.WriteAllText(path, combined);
        }

        private void PublishTag(PublishConfiguration pc, string tag)
        {
            var header = MakeHeader();
            var title = $"<h1>Tag: {tag}</h1>";
            var body = MakeTagTable(tag);

            var filename = $"tags/{MakeFilename(tag)}";
            var path = MakePath(pc, filename);
            var parts = new List<string>() { header, title, body };
            var combined = WrapInner(parts, true, $"Tag: {tag}", mainDivClass: "tag");
            System.IO.File.WriteAllText(path, combined);
        }

        private string MakeTagTable(string tag)
        {
            var sb = new StringBuilder();
            var header = "<table><thead><tr><th>Article<th>Body<th>Tags<th>Updated</thead><tbody>";
            sb.Append(header);
            using (var db = new FusekiContext())
            {
                var articleIds = db.Tags.Where(t => t.Name == tag).Select(el => el.ArticleId);
                var articles = db.Articles
                    .Include(ee => ee.Tags)
                    .Where(ee => ee.Published)
                    .Where(el => articleIds.Contains(el.Id))
                    .OrderBy(el => el.Title);
                
                foreach (var article in articles)
                {
                    var row = MakeArticleRowForList(article, tag);
                    sb.Append(row);
                }
            }
            var end = "</tbody></table>";
            sb.Append(end);
            return sb.ToString();
        }

        private string MakeArticleRowForList(Article article, string highlightTag)
        {
            var tags = MakeTagList(article, highlightTag, true);
            var link = LinkToArticle(article, true);
            return $"<tr><td>{link}<td>{article.Body.Length}<td>{tags}<td class='nb'>{article.Updated.ToString(MvcHelpers.DateFormat)}</tr>";
        }

        private static string MakeTagList(Article article, string highlightTag, bool inTagDir)
        {
            var sb = new StringBuilder();

            foreach (var tag in article.Tags.OrderBy(el => el.Name))
            {
                var line = MakeTagLink(tag, highlightTag, inTagDir);
                sb.Append(line);
            }
            return "<div class=taglist>"+sb.ToString()+"</div>";
        }

        private static string MakeTagLink(Tag tag, string highlightTag, bool inTagDir)
        {
            var taglink = $"{MakeFilename(tag.Name)}";
            var tagFolder = inTagDir ? "" : "tags/";
            var klass = tag.Name == highlightTag ? "highlight " : "";
            var line = $"<div class=\"{klass}tag\"><a class=taglink href=\"{tagFolder}{taglink}\">{tag.Name}</a></div>";
            return line;
        }

        private string MakeFooter(Article article)
        {
            var taglist = MakeTagList(article, "", false);
            var otherArticleLinks = MakeArticleLinks(article);

            return taglist + "<h2>Related Articles:</h2>" + otherArticleLinks;
        }

        private string MakeArticleLinks(Article article)
        {
            var res = ArticleData.GetRelatedArticles(article);
            var links = res.Select(el => ConvertRelatedArticleToLink(el)).ToList();
            return "<div class='articleLinks'>" + string.Join("\n<br />", links) + "</div>";
        }

        private static string ConvertRelatedArticleToLink(RelatedArticle el)
        {
            var articleLink = LinkToArticle(el.Article, false);
            var tagList = el.Article.Tags.OrderBy(el => el.Name).Select(t => MakeTagLink(t, el.RelatedTags.Select(el=>el.Name).Contains(t.Name) ? t.Name : "", false));

            return $"<div class='relatedarticle'>{articleLink} <div class=\"right\">{string.Join("", tagList)}</div></div>";
        }

        private static string MakePath(PublishConfiguration pc, string fn)
        {
            return pc.TempBase + "/" + fn;
        }

        public static string MakeFilename(string name, bool includeSuffix = true)
        {
            var res = "";
            foreach (var c in name)
            {
                if (AlphaNumeric.IsMatch(c.ToString()))
                {
                    res += c;
                }
            }
            if (includeSuffix)
            {
                return res + ".html";
            }
            return res;

        }

        private string WrapInner(List<string> parts, bool inTagDir, string title, string mainDivClass)
        {
            var stem = inTagDir ? "../" : "";
            var sb = new StringBuilder();
            var font = "<link href='https://fonts.googleapis.com/css?family=Vollkorn&display=swap' rel='stylesheet'>";
            var st = "<meta charset=\"utf-8\" /><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />" +
                $"<title>{title}</title><link rel=\"stylesheet\" href=\"https://stackpath.bootstrapcdn.com/bootstrap/4.3.1/css/bootstrap.min.css\" />" +
                $"\n{font}" +
                $"\n<script src='{stem}js/jquery.min.js'></script>\n" +
                $"\n<script src='{stem}js/jquery.tablesorter.js'></script>\n"+
                $"\n<script src='{stem}js/site-public.js'></script>\n" +
            $"<link rel=\"stylesheet\" href=\"{stem}css/site.css?{Settings.Version}\" /><body><div class=\"container {mainDivClass}\">";
            var ga= $"<script type='text/javascript'>var _gaq = _gaq || [ ];_gaq.push([ '_setAccount', '{Settings.GaId}' ]);_gaq.push([ '_trackPageview' ]);" +
         "(function() {" +
           "var ga = document.createElement('script'); ga.type = 'text/javascript'; ga.async = true; " +
           "ga.src = ('https:' == document.location.protocol ? 'https://ssl' : 'http://www') + '.google-analytics.com/ga.js'; " +
          " var s = document.getElementsByTagName('script')[ 0 ]; s.parentNode.insertBefore(ga, s); " +
         "})();</script>";
            var end = $"{ga}\n</div></body>";
            sb.Append(st);
            foreach (var part in parts)
            {
                sb.Append(part);
            }
            sb.Append(end);
            return sb.ToString();
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
        private static string MakeTitle(string title)
        {
            return $"<h1>{title}</h1>";
        }
    }
}
