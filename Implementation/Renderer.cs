using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Markdig;

using Microsoft.EntityFrameworkCore;

namespace FusekiC
{
    public class Renderer
    {
        private Regex LinkRegex = new Regex(@"(link:https?://)([\w-\%\&\=\/\.]+)");
        private Regex RemoteImageRegex = new Regex(@"\[(image:https?://([\w-\%\&\=\/\.]+))\]");
        private Regex LocalImageRegex = new Regex(@"\[(image:[\w-\%\&\=\/\.]+)\]");

        private MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
                .UsePipeTables()
                .Build();

        public string Normalize(string body)
        {
            if (string.IsNullOrEmpty(body))
            {
                body = "";
            }
            return Markdown.Normalize(body ?? "");
        }


        private static string MakeTitle(string title)
        {
            return $"<h1>{title}</h1>";
        }

        private string MakeFooter(Article article, bool inAdmin)
        {
            var taglist = MakeTagList(article, false, false, inAdmin);
            var otherArticleLinks = MakeArticleLinks(article, inAdmin);

            return taglist + "<h2>Related:</h2>" + otherArticleLinks;
        }

        private static string MakeTagList(Article article, bool highlightTag, bool inTagDir, bool inAdmin)
        {
            var sb = new StringBuilder();

            foreach (var tag in article.Tags.OrderBy(el => el.Name))
            {
                var line = MakeTagLink(tag, highlightTag, inTagDir, inAdmin);
                sb.Append(line);
            }
            return "<div class=taglist>" + sb.ToString() + "</div>";
        }

        private static string MakeTagLink(Tag tag, bool highlightTag, bool inTagDir, bool inAdmin)
        {
            var taglink = $"{tag.MakeFilename(inAdmin)}";
            var tagFolder = inTagDir ? "" : "tags/";
            var klass = highlightTag ? "highlight " : "";
            var tagCount = 0;
            using (var db = new FusekiContext())
            {
                //tagCount = db.Tags.Where(el => el.Name == tag.Name && el.Article.Published == true).Count();
            }

            var line = $"<div class=\"{klass}tag\"><a class=taglink href=\"{tagFolder}{taglink}\">{tag.Name}</a></div>";
            return line;
        }


        private string MakeArticleLinks(Article article, bool inAdmin)
        {
            var res = new ArticleData().GetRelatedArticles(article);
            var links = res.Select(el => ConvertRelatedArticleToLink(el, inAdmin)).ToList();
            return "<div class='articleLinks'>" + string.Join("\n<br />", links) + "</div>";
        }

        private static string ConvertRelatedArticleToLink(RelatedArticle el, bool inAdmin)
        {
            var articleLink = LinkToArticle(el.Article, false, inAdmin);

            //These is overlapping tags.
            var tagList = el.Article.Tags.OrderBy(el => el.Name)
                .Select(t => MakeTagLink(t, el.RelatedTags.Select(rt => rt.Id).Contains(t.Id), false, inAdmin));

            //todo: limit this to only showing overlapping tags.
            //.Select(t => MakeTagLink(t, el.RelatedTags.Where(el => el.Name.Contains(t.Name)), false, inAdmin));

            return $"<div class='relatedarticle'>{articleLink} <div class=\"right\">{string.Join("", tagList)}</div></div>";
        }

        public static string LinkToArticle(Article article, bool inTagDir, bool inAdmin)
        {
            if (inAdmin)
            {
                return $"<a class=articlelink href=\"/{article.Title}\">{article.Title}</a>";
            }
            else
            {
                var stem = inTagDir ? "../" : "";
                var fn = article.MakeFilename(inAdmin); //stem
                return $"<a class=articlelink href=\"{stem}{fn}\">{article.Title}</a>";
            }
        }

        private string WrapInner(Settings settings, List<string> parts, bool inTagDir, string title, string mainDivClass, bool includeAnalytics)
        {
            var stem = inTagDir ? "../" : "";
            var sb = new StringBuilder();
            var font = "<link href='https://fonts.googleapis.com/css?family=Vollkorn&display=swap' rel='stylesheet'>";
            var st = "<meta charset=\"utf-8\" /><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />" +
                $"<title>{title}</title><link rel=\"stylesheet\" href=\"https://stackpath.bootstrapcdn.com/bootstrap/4.3.1/css/bootstrap.min.css\" />" +
                $"\n{font}" +
                $"\n<script src='{stem}js/jquery.min.js'></script>\n" +
                $"\n<script src='{stem}js/jquery.tablesorter.js'></script>\n" +
                $"\n<script src='{stem}js/site-public.js'></script>\n" +
            $"<link rel=\"stylesheet\" href=\"{stem}css/site.css?{settings.Version}\" /><body><div class=\"container {mainDivClass}\">";
            var ga = "";
            if (includeAnalytics)
            {
                ga = $"<script type='text/javascript'>var _gaq = _gaq || [ ];_gaq.push([ '_setAccount', '{settings.GaId}' ]);_gaq.push([ '_trackPageview' ]);" +
             "(function() {" +
               "var ga = document.createElement('script'); ga.type = 'text/javascript'; ga.async = true; " +
               "ga.src = ('https:' == document.location.protocol ? 'https://ssl' : 'http://www') + '.google-analytics.com/ga.js'; " +
              " var s = document.getElementsByTagName('script')[ 0 ]; s.parentNode.insertBefore(ga, s); " +
             "})();</script>";
            }
            var end = $"{ga}\n</div></body>";
            sb.Append(st);
            foreach (var part in parts)
            {
                sb.Append(part);
            }
            sb.Append(end);
            return sb.ToString();
        }

        private string MakeArticleRowForList(Article article, bool highlightTag, bool inAdmin)
        {
            var tags = MakeTagList(article, highlightTag, true, inAdmin);
            var link = LinkToArticle(article, !inAdmin, inAdmin);
            return $"<tr><td>{link}<td>{article.Body.Length}<td>{tags}<td class='nb'>{article.Updated.ToString(MvcHelpers.DateFormat)}</tr>";
        }


        /// <summary>
        /// This is for tag viewing generically
        /// </summary>
        private string MakeTagTable(Tag tag, bool inAdmin)
        {
            var sb = new StringBuilder();
            var header = "<table><thead><tr><th>Article<th>Body<th>Tags<th>Updated</thead><tbody>";
            sb.Append(header);
            using (var db = new FusekiContext())
            {
                var articleIds = db.Tags.Where(t => t.Name == tag.Name).Select(el => el.ArticleId);
                var articles = db.Articles
                    .Include(ee => ee.Tags)
                    .Where(ee => ee.Published)
                    .Where(el => articleIds.Contains(el.Id))
                    .OrderBy(el => el.Title);

                foreach (var article in articles)
                {
                    var row = MakeArticleRowForList(article, false, inAdmin);
                    sb.Append(row);
                }
            }
            var end = "</tbody></table>";
            sb.Append(end);
            return sb.ToString();
        }

        public string GetTagString(Settings settings, Tag tag, bool inAdmin)
        {
            var title = $"<h1>Tag: {tag.Name}</h1>";
            var body = MakeTagTable(tag, inAdmin);

            var parts = new List<string>() { title, body };
            var combined = WrapInner(settings, parts, true, $"Tag: {tag}", mainDivClass: "tags", !inAdmin);
            return combined;
        }

        private string MakeHeader(Settings settings)
        {
            return $"<h1><a href='/'>{settings.SiteName}</a>";
        }


        public string GenerateArticleString(Settings settings, Article article, bool inAdmin)
        {
            var title = MakeTitle(article.Title);
            var body = ToHtml(article.Body, false);
            var footer = MakeFooter(article, inAdmin);
            var header = "";
            if (!inAdmin)
            {
                header = MakeHeader(settings);
            }
            var parts = new List<string>() { header, title, body, footer };
            var combined = WrapInner(settings, parts, false, title: article.Title, mainDivClass: "article", !inAdmin);

            return combined;
        }

        public string ToHtml(string body, bool isAdmin = false)
        {
            var htmlVersion = Markdown.ToHtml(body, MarkdownPipeline);
            htmlVersion = ReplaceLinks(htmlVersion);
            htmlVersion = ReplaceInternalLinks(htmlVersion, isAdmin);
            htmlVersion = AddImages(htmlVersion, isAdmin);
            return htmlVersion;
        }

        private string ReplaceLinks(string body)
        {
            var lines = body.Split("\r\n");
            var res = new List<string>();
            foreach (var line in lines)
            {
                var newLine = line;

                var matches = LinkRegex.Matches(newLine);
                foreach (Match match in matches)
                {
                    var fullLink = match.Groups[0].Value;
                    var actualLink = fullLink.Substring(5);
                    var htmlVersion = $"<a href={actualLink}>{actualLink}</a>";
                    newLine = newLine.Replace(fullLink, htmlVersion);
                }

                res.Add(newLine);
            }
            return string.Join("\r\n", res);
        }


        private string ReplaceInternalLinks(string body, bool isAdmin)
        {
            var lines = body.Split("\r\n");
            var res = new List<string>();
            foreach (var line in lines)
            {
                var newLine = line;
                var matches = Helpers.InternalLinkRegex.Matches(newLine);
                foreach (Match match in matches)
                {
                    var linkText = match.Groups[1].Value;
                    var article = FindArticleForInternalLink(linkText);
                    if (article == null)
                    {
                        //TODO proper error reporting here.
                        continue;
                    }
                    var link = LinkToArticle(article, inTagDir: false, inAdmin: isAdmin);
                    newLine = newLine.Replace("[" + linkText + "]", link);
                }

                res.Add(newLine);
            }
            return string.Join("\r\n", res);
        }

        public Article FindArticleForInternalLink(string text)
        {
            using (var db = new FusekiContext())
            {
                var candidates = db.Articles.Where(el => el.Title.ToLower().StartsWith(text.ToLower()));
                var best = candidates.OrderByDescending(el => el.Title.Length).FirstOrDefault();
                return best;
            }
        }

        private string AddImages(string body, bool inAdmin = false)
        {
            var lines = body.Split("\r\n");
            var res = new List<string>();

            foreach (var line in lines)
            {
                var newLine = line;
                var matches = LocalImageRegex.Matches(line);
                foreach (Match match in matches)
                {
                    var imageName = match.Groups[0].Value.Trim('[').Trim(']').Substring(6);
                    if (inAdmin)
                    {
                        var imageExists = TestImage(imageName, inAdmin);
                        if (imageExists)
                        {
                            var path = $"images/{imageName}";
                            var link = $"<img src='{path}'>";
                            var origMatch = $"[image:{imageName}]";
                            newLine = newLine.Replace(origMatch, link);
                        }
                    }
                    else
                    {
                        var ae = 3;
                        var path = $"images/{imageName}";
                        var link = $"<img src='{path}'>";
                        var origMatch = $"[image:{imageName}]";
                        newLine = newLine.Replace(origMatch, link);
                    }
                }

                res.Add(newLine);
            }
            return string.Join("\r\n", res);
        }

        private static bool TestImage(string name, bool inAdmin)
        {
            if (inAdmin)
            {
                return System.IO.File.Exists($"../fusekiimages/{name}");
            }
            else
            {
                return System.IO.File.Exists($"../images/{name}");
            }
        }

    }
}
