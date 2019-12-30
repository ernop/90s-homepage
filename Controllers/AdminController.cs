using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static FusekiC.Helpers;

namespace FusekiC
{
    [Authorize]
    public class AdminController : Controller
    {
        private ArticleData ArticleData { get; set; }
        public Renderer Renderer { get; set; }
        public AdminController(Renderer renderer)
        {
            ArticleData = new ArticleData();
            Renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        }

        [HttpGet("/")]
        public IActionResult Index()
        {
            return RedirectToAction("List", "Admin");
        }

        [HttpPost("/massAdd")]
        public IActionResult MassAdd(string baseDir)
        {
            if (!System.IO.Directory.Exists(baseDir))
            {
                return Redirect("../");
            }
            var files = System.IO.Directory.EnumerateFiles(baseDir);
            foreach (var file in files)
            {
                if (!file.EndsWith("rst") && !file.EndsWith("rsx"))
                {
                    continue;
                }

                if (file.EndsWith("rst"))
                {
                    var fp = file;
                    ProcessRst(fp);
                }
                if (file.EndsWith("rsx"))
                {
                    ProcessRsx(file);
                }
                
            }
            return Redirect("..");
        }

        private void ProcessRst(string fp)
        {
            Process(fp, false);
        }
        private void ProcessRsx(string fp)
        {
            Process(fp, false);
        }

        public void Process(string fp, bool published)
        { 
            var lines = System.IO.File.ReadLines(fp);
            if (!lines.Any())
            {
                return;
            }
            if (lines.Count() == 1)
            {
                return;
            }
            var title = lines.Skip(1)?.Take(1).First().Trim();
            if (string.IsNullOrEmpty(title))
            {
                return;
            }

            using (var db = new FusekiContext())
            {
                var exi = db.Articles.FirstOrDefault(el => el.Title == title);
                if (exi != null)
                {
                    return;
                }

                var tagline = lines.Last();
                while (string.IsNullOrEmpty(tagline))
                {
                    lines = lines.Take(lines.ToList().Count - 1);
                    tagline = lines.Last();
                }

                if (!tagline.StartsWith("tags:"))
                {
                    tagline = "tags:";
                }
                var tags = tagline.Split("tags:")[1].Trim().Split(",").Select(el => el.Trim()).ToList();
                
                lines = lines.Skip(4);
                lines = lines.Take(lines.ToList().Count - 1);

                var now = DateTime.Now;
            
                var article = new Article();
                article.Title = title;
                article.Body = string.Join("\n", lines);
                article.Created = now;
                article.Updated = now;
                article.Published = published;

                db.Add(article);
                db.SaveChanges();
                var articleEntity = db.Articles.First(el => el.Title == title);
                foreach (var tagname in tags)
                {
                    var tag = new Tag();
                    tag.ArticleId = articleEntity.Id;
                    tag.Name = tagname;
                    tag.Updated = now;
                    tag.Created = now;
                    db.Add(tag);
                };
                db.SaveChanges();
            }

        }


        [HttpPost("/search")]
        public IActionResult Search(string term)
        {
            var model = new SearchResultModel(Renderer);
            term = term.ToLower();
            model.Term = term;

            using (var db = new FusekiContext())
            {
                var art = db.Articles
                    .Include(el=>el.Tags)
                    .Where(el => el.Title.ToLower().Contains(term));
                model.TitleMatches = art.ToList();
                
                var body = db.Articles
                    .Include(el => el.Tags)
                    .Where(el => el.Body.ToLower().Contains(term));
                model.BodyMatches = body.ToList();

                //var tags = db.Tags.Where(el => el.Name.ToLower().Contains(term));
                //model.TagMatches = tags.ToList();
            }
            ViewData["Title"] = $"Search for: {term}";
            return View("SearchResult", model);
        }


        [HttpGet("/tag/{name}")]
        public IActionResult Tag(string name)
        {
            using (var db = new FusekiContext())
            {
                var tag = db.Tags.Where(el => el.Name == name).FirstOrDefault();
                if (tag == null)
                {
                    return Redirect("../../");
                }
                var articleIds = db.Tags.Where(el => el.Name == name).Select(el => el.ArticleId);
                var articles = db.Articles
                    .Where(el => articleIds.Contains(el.Id))
                    .Where(el=>el.Published)
                    .Include(el => el.Tags)
                    .ToList();
                var model = new ListModel($"Tag: {tag.Name}", articles, name);
                ViewData["Title"] = $"Search: {name}";
                return View("List", model);
            }
        }

        [HttpGet("/create")]
        public IActionResult CreateArticle()
        {
            using (var db = new FusekiContext())
            {
                var article = new Article();
                var rnd = new System.Random();
                article.Title = $"draft{rnd.Next(1000)}";
                article.Body = "";
                var a = db.Add(article);

                db.SaveChanges();
                return Redirect($"article/edit/{a.Entity.Title}");
            }
        }

        [HttpGet("/article/edit/{title}"), HttpPost("/article/edit/{title}")]
        public IActionResult EditArticle(string title)
        {
            using (var db = new FusekiContext())
            {
                var article = db.Articles
                    .Include(el => el.Tags)
                    .First(el => el.Title.StartsWith(title));
                //startswith to allow titles with ? marks in them. Doh.

                var normalized = Renderer.Normalize(article.Body);
                article.Body = normalized;
                var model = new ArticleModel(article, Renderer.ToHtml(normalized));
                ViewData["Title"] = $"Editing {article.Title}";
                return View("EditArticle", model);
            }
        }

        [HttpPost("/article/update")]
        public IActionResult UpdateArticle(ArticleModel model)
        {
            if (ModelState.IsValid)
            {
                using (var db = new FusekiContext())
                {
                    var article = db.Articles
                        .Include(aa => aa.Tags)
                        .First(el => el.Id == model.Id);
                    //allow extended chars in title. But links and finding will strip them.
                    article.Title = model.Title;

                    var normalized = Renderer.Normalize(model.Body);
                    article.Body = normalized;

                    var now = DateTime.Now;
                    article.Updated = now;

                    if (article.Created == DateTime.MinValue)
                    {
                        article.Created = now;
                    }

                    //rectify tags.
                    var empty = new List<string>();
                    var tags = model.Tags?.Split(",").Select(el => el.Trim()) ?? new string[0];

                    var newTagNames = tags
                        .Select(el => el.Trim())
                        .Where(ee => !string.IsNullOrWhiteSpace(ee))
                        .ToHashSet();

                    var existingTags = article.Tags.Select(el => el.Name).ToHashSet();

                    var todelete = existingTags.Except(newTagNames);
                    var toadd = newTagNames.Except(existingTags);

                    foreach (var bad in todelete)
                    {
                        var tag = db.Tags.Where(el => el.ArticleId == article.Id && el.Name == bad).First();
                        db.Remove(tag);
                    }

                    foreach (var good in toadd)
                    {
                        var tag = CreateTag(article.Id, good);
                        db.Add(tag);
                    }
                    db.SaveChanges();
                    return Redirect($"../{model.Title}");
                }
            }
            else
            {
                return View();
            }
        }


        [HttpGet("/tags")]
        public IActionResult TagList()
        {
            var m = GetMetatagListModel();
            return View("TagList", m);
        }

        [HttpGet("list")]
        public IActionResult List()
        {
            using (var db = new FusekiContext())
            {
                var articles = db.Articles
                    .Where(el => el.Deleted == false)
                    .Where(el => el.Title != null && el.Title.Length > 0)
                    .Include(ee => ee.Tags)
                    .ToList();
                var m = new ListModel("All", articles);
                ViewData["Title"] = $"All articles";
                return View("List", m);
            }
        }

        [HttpGet("/published")]
        public IActionResult ListPublished()
        {
            using (var db = new FusekiContext())
            {
                var articles = db.Articles
                    .Where(el => el.Published == true)
                    .Where(el => el.Title != null && el.Title.Length > 0)
                    .Include(ee => ee.Tags)
                    .ToList();
                var m = new ListModel("All", articles);
                ViewData["Title"] = $"Published articles";
                return View("List", m);
            }
        }

        [HttpGet("/unpublished")]
        public IActionResult ListUnpublished()
        {
            using (var db = new FusekiContext())
            {
                var articles = db.Articles
                    .Where(el => el.Deleted == false)
                    .Where(el => el.Published == false)
                    .Where(el => el.Title != null && el.Title.Length > 0)
                    .Include(ee => ee.Tags)
                    .ToList();
                var m = new ListModel("All", articles);
                ViewData["Title"] = $"Unpublished articles";
                return View("List", m);
            }
        }

        [HttpGet("/listall")]
        public IActionResult ListAll()
        {
            using (var db = new FusekiContext())
            {
                var articles = db.Articles
                    .Where(el => el.Title != null && el.Title.Length > 0)
                    .Include(ee => ee.Tags)
                    .ToList();
                var m = new ListModel("All", articles);
                ViewData["Title"] = $"All articles";
                return View("List", m);
            }
        }

        [HttpGet("/{title}")]
        public IActionResult ViewArticle(string title)
        {
            using (var db = new FusekiContext())
            {
                var article = db.Articles
                    .Include(ee => ee.Tags)
                    .FirstOrDefault(el => el.Title.StartsWith(title));
                //startswith to fix question mark thing.

                if (article == null)
                {
                    return RedirectToAction("List"); ;
                }

                var related = ArticleData.GetRelatedArticles(article);

                var model = new ArticleModel(article, Renderer.ToHtml(article.Body, true), related);
                ViewData["Title"] = $"{article.Title}";
                return View("Article", model);
            }
        }

        private static Tag CreateTag(int articleId, string name)
        {
            var tag = new Tag();
            tag.ArticleId = articleId;
            tag.Name = name.Trim().ToLower();
            var now = DateTime.Now;
            tag.Created = now;
            tag.Updated = now;
            return tag;
        }
    }
}