using System.Collections.Generic;

namespace FusekiC
{
    public class SearchResultModel
    {
        public SearchResultModel(Renderer renderer)
        {
            Renderer = renderer;
        }
        public SearchResultModel() { }

        public Renderer Renderer { get; set; }
        public string Term { get; set; }
        public List<Article> TitleMatches { get; set; }
        public List<Article> BodyMatches { get; set; }
        public List<Tag> TagMatches { get; set; }
    }
}
