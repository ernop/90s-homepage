using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace FusekiC
{
    public class ArticleData
    {
        /// <summary>
        /// Weigh them by inverse popularity of related tag, not by count strictly.
        /// </summary>
        public List<RelatedArticle> GetRelatedArticles(Article article)
        {
            var tagnames = article.Tags.Select(el => el.Name);
            var counts = new Dictionary<string, int>();
            using (var db = new FusekiContext())
            {
                foreach (var tag in tagnames)
                {
                    var count = db.Tags.Where(el => el.Name == tag).Count();
                    counts[tag] = count;
                }
                //calculate the related tags, and then the score of every other article in the system.

                var scoredArticles = new Dictionary<Article, double>();

                foreach (var otherArticle in db.Articles
                    .Include(el=>el.Tags)
                    .Where(el => el.Published))
                {
                    if (otherArticle.Id == article.Id)
                    {
                        continue;
                    }
                    var uniqs = otherArticle.Tags.Select(el => el.Name).ToHashSet();
                    uniqs.IntersectWith(tagnames);
                    var score = uniqs.Select(el => 1.0 / counts[el]).Sum();
                    scoredArticles[otherArticle] = score;
                }

                var orderedRelated = scoredArticles.ToList().OrderByDescending(el => el.Value);
                var res = new List<RelatedArticle>();
                foreach (var el in orderedRelated)
                {
                    if (el.Value == 0)
                    {
                        break;
                    }
                    if (res.Count > 10)
                    {
                        break;
                    }
                    var relatedArticle = db.Articles
                        .Include(el => el.Tags)
                        .FirstOrDefault(ra => ra.Id == el.Key.Id);

                    //doh, calculating these again.
                    //var relatedTags = relatedArticle.Tags.Where(el => tagnames.Contains(el.Name)).ToList();

                    res.Add(new RelatedArticle(relatedArticle, relatedArticle.Tags));
                }

                return res;
            }
        }

        /// <summary>
        /// TODO: what's the difference?
        /// </summary>
        public List<RelatedArticle> GetRelatedArticlesNaive(Article article)
        {
            var tagnames = article.Tags.Select(el => el.Name);
            using (var db = new FusekiContext())
            {
                var relatedTags = db.Tags.Include(el => el.Article).Where(el => tagnames.Contains(el.Name));
                var scores = new Dictionary<int, List<Tag>>();
                foreach (var tag in relatedTags)
                {
                    if (tag.ArticleId == article.Id)
                    {
                        continue;
                    }
                    if (!tag.Article.Published)
                    {
                        continue;
                    }
                    if (!scores.ContainsKey(tag.ArticleId))
                    {
                        scores[tag.ArticleId] = new List<Tag>();
                    }
                    scores[tag.ArticleId].Add(tag);
                }

                var orderedRelated = scores.ToList().OrderByDescending(el => el.Value.Count);
                var res = new List<RelatedArticle>();
                foreach (var el in orderedRelated)
                {
                    if (el.Value.Count == 0)
                    {
                        break;
                    }
                    if (res.Count > 10)
                    {
                        break;
                    }
                    //var relatedArticle = db.Articles.Find(el.Key);

                    var relatedArticle = db.Articles.FirstOrDefault(ra => ra.Id == el.Key);

                    res.Add(new RelatedArticle(relatedArticle, el.Value));
                }

                return res;
            }
        }
    }
}
