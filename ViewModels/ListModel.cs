using System;
using System.Linq;

using System.Collections.Generic;

namespace FusekiC
{
    public class ListModel
    {
        public ListModel() { }
        public ListModel(string name, List<Article> articles, string highlightTag = "")
        {
            Name = name;
            Articles = articles.Select(el=>new ArticleModel(el, "", "", "")).ToList();
            HighlightTag = highlightTag;
        }
        public string Name { get; set; }
        public List<ArticleModel> Articles { get; set; }
        public string HighlightTag { get; set; }
    }

    public class MetatagListModel
    {
        /// <summary>
        /// what type taglist filter this is.
        /// </summary>
        /// 

        public MetatagListModel(string name, List<Metatag> metatags)
        {
            Name = name;
            Metatags = metatags;
            
        }

        public string Name { get; set; }
        public List<Metatag> Metatags { get; set; }
    }

    public class Metatag
    {
        public string Name { get; set; }
        public int Count { get; set; }
        public DateTime Updated { get; set; }
        public DateTime Created { get; set; }
        public List<Article> Articles { get; set; }
    }
}