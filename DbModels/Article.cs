using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

using Microsoft.AspNetCore.Mvc.TagHelpers;

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

        public string MakeFilename(bool inAdmin)
        {
            var res = "";
            foreach (var c in Title)
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

        public string MakeOldFilename(bool inAdmin)
        {
            var res = "";
            foreach (var c in Title)
            {
                if (Helpers.AlphaNumeric.IsMatch(c.ToString()))
                {
                    res += c;
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