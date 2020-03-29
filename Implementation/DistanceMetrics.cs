using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FusekiC
{

    public static class Comparators
    {
        public static double GetLengthDistance(Article a, Article b)
        {
            return Math.Abs(a.Title.Split(" ").Length - b.Title.Split(" ").Length);
        }

        /// <summary>
        /// This works reasonably well, but doesn't penalize connections for having lots of tags NOT in common.
        /// It's also annoying that 2 is a constant, and that having increased similarity in tags doesn't help much.  i.e. 1=>2 goes in value from 1 to 1/2 to 1/3, not much when 1's are floating around
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double GetTagCommonality(Article a, Article b)
        {
            using (var db = new FusekiContext())
            {
                var atags = a.Tags.Select(el => el.Name);
                var btags = b.Tags.Select(el => el.Name);
                var c = atags.Intersect(btags);
                var incommon = c.Count();
                if (incommon == 0)
                {
                    return 2;
                }
                return 1/incommon;
            }
        }

        public static string ArticleKeyLookup(Article a, Article b)
        {
            var order = new List<string>() { a.Title, b.Title };
            order.OrderBy(el => el);

            if (order[0] == b.Title)
            {
                //fixed order;
                var c = a;
                a = b;
                b = c;
            }
            return $"{a.Title}___{b.Title}";
        }
    }

    public class DistanceMetrics<T>
    {
        private Func<T, T, double> Funct { get; set; }

        private Func<T,T,string> KeyLookup { get; set; }

        public DistanceMetrics(Func<T, T, double> met, Func<T,T,string> keyLookup)
        {
            Funct = met;
            KeyLookup = keyLookup;
        }
        
        /// <summary>
        /// Building blocks: first create a distance metric for articles.  Once you have that you can apply normal partitioning to it.
        /// Testing method for this: give a really obvious distance metric, like length of title, then run the partitioner on it.
        /// </summary>
        public Dictionary<string, double> Distances { get; set; } = new Dictionary<string, double>();

        private double CalculateDistance(T a, T b)
        {
            //we do not reset the cache, so using the site and adding tags will cause invalid data to be used.
            return Funct(a, b);
        }

        public double GetDistance(T a, T b)
        {
            var lookup = KeyLookup(a, b);
            if (!Distances.ContainsKey(lookup))
            {
                Distances[lookup] = CalculateDistance(a, b);
            }

            return Distances[lookup];
        }
    }
}
