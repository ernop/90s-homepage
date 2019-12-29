using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace FusekiC
{
    public class Tag
    {
        public int Id { get; set; }
        public int ArticleId { get; set; }
        public string Name { get; set; }

        public DateTime Created { get; set; }

        public DateTime Updated { get; set; }

        public override string ToString()
        {
            return $"{Name}";
        }
    }
}