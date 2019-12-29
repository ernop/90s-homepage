using System.Linq;
using System.Collections.Generic;

namespace FusekiC
{
    public class TagViewModel
    {
        public TagViewModel(string tags, string highlightTag)
        {
            Tags = tags.Split(',').ToList();
            HighlightTag = highlightTag;
        }

        public TagViewModel(List<Tag> tags, string highlightTag)
        {
            Tags = tags.Select(ee=>ee.Name).ToList();
            HighlightTag = highlightTag;
        }

        public List<string> Tags { get; set; }
        public string HighlightTag { get; set; }
    }
}
