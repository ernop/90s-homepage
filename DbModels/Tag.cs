using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace FusekiC
{
    public class Tag
    {
        public int Id { get; set; }
        public Article Article { get; set; }
        public int ArticleId { get; set; }
        public string Name { get; set; }

        public DateTime Created { get; set; }

        public DateTime Updated { get; set; }

        public override string ToString()
        {
            return $"{Name}";
        }

        public string MakeFilename(bool inAdmin)
        {
            var res = "";
            foreach (var c in Name)
            {
                if (Helpers.AlphaNumeric.IsMatch(c.ToString()))
                {
                    res += c;
                }
                if (string.IsNullOrWhiteSpace(c.ToString()))
                {
                    res += "-";
                }
            }
            if (!inAdmin)
            {
                return res + ".html";
            }
            return res;
        }
    }
}