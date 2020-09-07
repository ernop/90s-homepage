using System;

namespace FusekiC
{
    public class ImageTag
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Image Image { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }

        public override string ToString()
        {
            return $"ImageTag: {Image.Filename}: {Name}";
        }

    }
}