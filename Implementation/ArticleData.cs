using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace FusekiC
{
    public class ArticleData
    {
        public List<RelatedArticle> GetRelatedArticles(Article article)
        {
            var tagnames = article.Tags.Select(el => el.Name);
            using (var db = new FusekiContext())
            {
                var relatedTags = db.Tags.Include(el=>el.Article).Where(el => tagnames.Contains(el.Name));
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
                    var relatedArticle = db.Articles.Find(el.Key);
                    res.Add(new RelatedArticle(relatedArticle, el.Value));
                }

                return res;
            }
        }
    }
}
