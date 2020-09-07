using System;
using System.Collections.Generic;
using System.Linq;

namespace FusekiC
{
    public class Image
    {
        public int Id { get; set; }
        public string Filename { get; set; }
        public List<ImageTag> ImageTags { get; set; }
        public bool Deleted { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        public override string ToString()
        {
            return $"Image: {Filename} {GetTagString()}";
        }

        public string GetTagString()
        {
            var combined = string.Join(",", ImageTags.Select(el => el.Name));
            return combined;
        }
    }
}