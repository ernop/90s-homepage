using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Markdig;

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
                    var link = Publisher.LinkToArticle(article, inTagDir: false, inAdmin: isAdmin);
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
