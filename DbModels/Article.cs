using System;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace FusekiC
{
    public class Article
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public bool Deleted { get; set; } = false;
        public bool Published { get; set; } = false;
        public List<Tag> Tags { get; } = new List<Tag>();
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }

        public override string ToString()
        {
            return $"{Title} ({Body.Length})";
        }
    }
}