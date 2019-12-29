using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Markdig;

namespace FusekiC
{
    public class ArticleModel
    {
        public ArticleModel() { }

        public ArticleModel(Article ar, string renderedBody, List<RelatedArticle> related = null)
        {
            Id = ar.Id;
            Title = ar.Title;
            Body = ar.Body;
            Tags = string.Join(",", ar.Tags.Select(ee => ee.Name).OrderBy(el => el).ToList());
            Published = ar.Published;
            Deleted = ar.Deleted;
            Updated = ar.Updated;
            Created = ar.Created;
            RelatedArticles = related;
            RenderedBody = renderedBody;
        }

        public int Id { get; set; }

        [StringLength(100, MinimumLength = 1, ErrorMessage = "Length from 1 to 100")]
        public string Title { get; set; }
        public string Body { get; set; }
        public string RenderedBody { get; set; }
        public string Tags { get; set; }
        public bool Published { get; set; }
        public bool Deleted { get; set; }
        public DateTime Updated { get; set; }
        public DateTime Created { get; set; }
        public List<RelatedArticle> RelatedArticles { get; set; }

    }
}
