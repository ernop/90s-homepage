using System.Collections.Generic;

namespace FusekiC
{
    public class RelatedArticle
    {
        public RelatedArticle(Article article, List<Tag> relatedTags)
        {
            Article = article;
            RelatedTags = relatedTags;
        }
        public Article Article { get; set; }
        public List<Tag> RelatedTags { get; set; }
    }
}
