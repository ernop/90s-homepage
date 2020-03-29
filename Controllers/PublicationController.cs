using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGeneration;
using static FusekiC.Helpers;

namespace FusekiC
{
    [Authorize]
    public class PublicationController : Controller
    {
        private Publisher Publisher { get; set; }
        private Renderer Renderer { get; set; }
        private ConsoleLogger Logger { get; set; }
        private Settings Settings { get; set; }
        private PublishConfiguration PublishConfiguration { get; set; }
        public PublicationController(Renderer renderer, ConsoleLogger logger, Settings settings, PublishConfiguration pc)
        {
            Logger = logger;
            Settings = settings;
            Renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            PublishConfiguration = pc;
            Publisher = new Publisher(Renderer, logger, Settings, PublishConfiguration);
        }

        [HttpGet("/listpublications")]
        public IActionResult Index()
        {
            using (var db = new FusekiContext())
            {
                var pubs = db.Publications.OrderByDescending(el => el.Id).ToList();
                var model = new PublicationListModel(pubs);
                ViewData["Title"] = $"Publications";
                return View("PublicationList", model);
            }
        }

        [HttpPost("/publish")]
        public IActionResult Publish()
        {
            var model = new PublicationResultModel();

            var publishResult = Publisher.Publish();

            var publication = new Publication();
            publication.ArticleCount = publishResult.Count;
            publication.PublicationTime = DateTime.Now;
            using (var db = new FusekiContext())
            {
                db.Add(publication);
                db.SaveChanges();
            }
            model.Publication = publication;
            ViewData["Title"] = $"PublicationResult";
            return View("PublicationResult", model);
        }

        [HttpPost("/partition")]
        public IActionResult Partition()
        {
            //var partitioner = new Partitioner<Article>((a, b) => Comparators.GetLengthDistance(a, b), (a, b) => Comparators.ArticleKeyLookup(a, b));
            var partitioner = new Partitioner<Article>((a, b) => Comparators.GetTagCommonality(a, b), (a, b) => Comparators.ArticleKeyLookup(a, b));
            
            using (var db = new FusekiContext())
            {
                var articles = db.Articles.Where(el => el.Published == true)
                    .Include(el => el.Tags).ToList();
                var r = new Random();
                articles = articles.OrderBy(el => r.NextDouble()).ToList();
                var partitiondata = partitioner.GetPartitions(25, articles);
                var model = new ArticlePartitionModel(partitiondata);
                return View("ArticlePartitions", model);
            }
            
        }
    }
}