using System;
using System.Collections.Generic;
using System.Linq;
using Accord.MachineLearning;
using Accord.Statistics.Filters;
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
        public IActionResult Partition(int partitionCount)
        {
            //var partitioner = new Partitioner<Article>((a, b) => Comparators.GetLengthDistance(a, b), (a, b) => Comparators.ArticleKeyLookup(a, b));
            var partitioner = new Partitioner<Article>((a, b) => Comparators.GetTagCommonality(a, b), (a, b) => Comparators.ArticleKeyLookup(a, b));

            using (var db = new FusekiContext())
            {
                var articles = db.Articles.Where(el => el.Published == true)
                    .Include(el => el.Tags).ToList();
                var r = new Random();
                articles = articles.OrderBy(el => r.NextDouble()).ToList();
                var partitiondata = partitioner.GetPartitions(partitionCount, articles);
                var model = new ArticlePartitionModel(partitiondata);
                return View("ArticlePartitions", model);
            }
        }

        //[HttpPost("/partition3")]
        //public IActionResult Partition3(int partitionCount)
        //{
        //    using (var db = new FusekiContext())
        //    {
        //        var articles = db.Articles.Where(el => el.Published == true)
        //            .Include(el => el.Tags).Take(3).ToList();
        //        //get a tag vector for each article.

        //        var allTags = new HashSet<string>();

        //        //TODO: What happens if we remove all tags which only occur once.
        //        foreach (var article in articles)
        //        {
        //            foreach (var tag in article.Tags)
        //            {
        //                allTags.Add(tag.Name);
        //            }
        //        }

        //        var newAllTags = new HashSet<string>();
        //        foreach (var t in allTags)
        //        {
        //            var relatedArticles = db.Articles.Where(el => el.Tags.Select(tag => tag.Name).Contains(t));
        //            if (relatedArticles.Count() > 1)
        //            {
        //                newAllTags.Add(t);
        //            }
        //        }

        //        allTags = newAllTags;

        //        var allTagsOrdered = allTags.OrderBy(el => el);

        //        var obs = new List<List<double>>();
        //        var dict = new Dictionary<string, object>();

        //        foreach (var article in articles)
        //        {
        //            var articleTags = article.Tags.Select(el => el.Name);
        //            var vector = new List<double>();
        //            foreach (var tag in allTagsOrdered)
        //            {
        //                if (articleTags.Contains(tag))
        //                {
        //                    vector.Add(1);
        //                }
        //                else
        //                {
        //                    vector.Add(0);
        //                }
        //            }
        //            obs.Add(vector);
        //        }

        //        var n = obs.Count;
        //        var m = obs[0].Count;

        //        double[,] test = new double[n, m];

        //        var ii = 0;
        //        foreach (var el in obs)
        //        {
        //            var jj = 0;
        //            foreach (var item in el)
        //            {
        //                test[ii, jj] = item;
        //                jj++;
        //            }
        //            ii++;
        //        }
        //        //TODO: confirm this is converting data correctly.

                
        //        alglib.clusterizerstate s;
        //        alglib.ahcreport rep;
        //        int[] cidx;
        //        int[] cz;
        //        alglib.clusterizercreate(out s);
        //        alglib.clusterizersetpoints(s, test, 2);
        //        var q = test.ToString();
        //        alglib.clusterizersetahcalgo(s, 0);
        //        alglib.clusterizerrunahc(s, out rep);
        //        var res = alglib.clusterizergetkclusters(rep, partitionCount, out cidx, out cz);
        //        //todo: is there a way to convert a dendogram to N clusters? Or just keep it that way?
        //        return null;
        //    }
        //}


        [HttpPost("/partition2")]
        public IActionResult Partition2(int partitionCount)
        {
            using (var db = new FusekiContext())
            {
                var articles = db.Articles.Where(el => el.Published == true)
                    .Include(el => el.Tags).Take(20).ToList();
                //get a tag vector for each article.

                var allTags = new HashSet<string>();

                //TODO: What happens if we remove all tags which only occur once.
                foreach (var article in articles)
                {
                    foreach (var tag in article.Tags)
                    {
                        allTags.Add(tag.Name);
                    }
                }

                var newAllTags = new HashSet<string>();
                foreach (var t in allTags)
                {
                    var relatedArticles = db.Articles.Where(el => el.Tags.Select(tag => tag.Name).Contains(t));
                    if (relatedArticles.Count() > 1)
                    {
                        newAllTags.Add(t);
                    }
                }

                allTags = newAllTags;

                var allTagsOrdered = allTags.OrderBy(el => el);

                var obs = new List<List<double>>();
                var dict = new Dictionary<string, object>();

                foreach (var article in articles)
                {
                    var articleTags = article.Tags.Select(el => el.Name);
                    var vector = new List<double>();
                    foreach (var tag in allTagsOrdered)
                    {
                        if (articleTags.Contains(tag))
                        {
                            vector.Add(1);
                        }
                        else
                        {
                            vector.Add(0);
                        }
                    }
                    obs.Add(vector);
                }

                var vecvec = obs.Select(el => el.ToArray()).ToArray();

                var kmeans = new KMeans(k: partitionCount);

                var clusters = kmeans.Learn(vecvec);
                dict["Kmeans Error"] = kmeans.Error;
                dict["dimensionality"] = kmeans.Dimension;
                dict["Iterations"] = kmeans.Iterations;
                dict["MaxIterations"] = kmeans.MaxIterations;
                dict["Tolerance"] = kmeans.Tolerance;


                int[] labels = clusters.Decide(vecvec);
                //labels is array[articleId] => partitionNumber
                var ii = 0;
                var psets = new List<PartitionSet<Article>>();

                //this is totally fake. TODO: refactor these to be dumber - no need to have comparators etc.
                var dm = new DistanceMetrics<Article>((a, b) => Comparators.GetTagCommonality(a, b), (a, b) => Comparators.ArticleKeyLookup(a, b));
                while (ii < partitionCount)
                {
                    //TODO: is accord zero indexed?
                    psets.Add(new PartitionSet<Article>(dm, ii));
                    ii++;
                }
                var index = 0;
                foreach (var l in labels)
                {
                    var article = articles[index];
                    index++;
                    psets[l].Add(article);
                }


                var partitiondata = new PartitionData<Article>(psets, dict);

                var model = new ArticlePartitionModel(partitiondata);
                return View("ArticlePartitions", model);
            }

        }
    }
}