using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGeneration;
using static FusekiC.Helpers;

namespace FusekiC
{
    [Authorize]
    public class AjaxController : Controller
    {
        private Renderer Renderer { get; set; }
        private ConsoleLogger Logger { get; set; }
        public AjaxController(Renderer renderer, ConsoleLogger logger)
        {
            Logger = logger;
            Renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        }

        [HttpPost("/ajax/search")]
        public IActionResult Search(string term)
        {
            Logger.LogMessage($"Searched for: {term}");
            var model = new SearchResultModel(Renderer);
            term = term.ToLower();
            model.Term = term;

            using (var db = new FusekiContext())
            {
                var art = db.Articles.Where(el => el.Title.ToLower().Contains(term));
                model.TitleMatches = art.ToList();

                var body = db.Articles.Where(el => el.Body.ToLower().Contains(term));
                model.BodyMatches = body.ToList();

                //var tags = db.Tags.Where(el => el.Name.ToLower().Contains(term));
                //model.TagMatches = tags.ToList();
            }

            return new JsonResult(model);
        }



        [HttpPost("/ajax/article/publishtoggle/{id:int}")]
        public IActionResult PublishArticle(int id)
        {
            Logger.LogMessage($"PublishToggle: {id}");
            using (var db = new FusekiContext())
            {
                var article = db.Articles.First(el => el.Id == id);
                if (article == null)
                {
                    return new JsonResult(new { Message = "No article" });
                }
                if (article.Published)
                {
                    article.Published = false;
                    db.SaveChanges();
                }
                else
                {
                    if (article.Deleted)
                    {
                        return new JsonResult(new { Message = "Deleted alread" });
                    }
                    article.Published = true;
                    db.SaveChanges();
                }
                return new JsonResult(new { Message = "Success", Status = article.Published });
            }
        }

        [HttpPost("/ajax/article/deletetoggle/{id:int}")]
        public IActionResult DeleteArticle(int id)
        {
            Logger.LogMessage($"DeleteToggle: {id}");
            using (var db = new FusekiContext())
            {
                var article = db.Articles.First(el => el.Id == id);
                if (article == null)
                {
                    return new JsonResult(new { Message = "No article" });
                }
                if (article.Deleted)
                {
                    article.Deleted = false;
                    db.SaveChanges();

                }
                else
                {
                    article.Deleted = true;
                    article.Published = false;
                    db.SaveChanges();

                }
                return new JsonResult(new { Message = "Success", Status = article.Deleted });
            }
        }
    }
}