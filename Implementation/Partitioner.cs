using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FusekiC
{
    /// <summary>
    /// For two string inputs, how to determine their similarity to each other?
    /// </summary>
    public enum PartitionMethod
    {
        //length difference between two texts
        TitleLength = 1,
        TagSimilarity = 2,
    }

    /// <summary>
    /// a partitioner has a set partitionmethod.
    /// </summary>
    public partial class Partitioner<T>
    {
        private DistanceMetrics<T> metrics;

        public Partitioner(Func<T, T, double> comparator, Func<T, T, string> keyLookup)
        {
            metrics = new DistanceMetrics<T>(comparator, keyLookup);
        }

        ///obviously impossible to test every possibility.
        ///Might be possible to iterate or do local hill climbing?  order matters for all that.
        ///title length has weird transitivity; don't use that because it won't apply for other metrics.
        ///I could just do it 100 times with random starts, iterating adding members to the set which they are closest to.
        ///Then as a final step test each element to see if it belongs better in another set.

        public PartitionData<T> GetPartitions(int partitionCount, List<T> Elements)
        {
            if (partitionCount < 2 || partitionCount > 100)
            {
                throw new Exception("Are you sure you want to generate that many partitions?");
            }

            var sets = new List<PartitionSet<T>>();
            var ii = 0;

            while (ii < partitionCount)
            {
                var px = new PartitionSet<T>(metrics, ii);
                sets.Add(px);
                ii++;
            }

            using (var db = new FusekiContext())
            {
                foreach (var el in Elements)
                {
                    var targetSet = FindBestPartition(el, sets);
                    targetSet.Add(el);
                }
            }

            //initial assignment done.
            //now iterate over each item, removing it and then readding it where it belongs til we reach stability or N iterations.
            var loopCt = 0;
            var moveCt = 100;

            var stats = new Dictionary<string, object>();
            stats["InitialQuality"] = FindQuality(sets);

            while (loopCt < 200)
            {
                moveCt = 0;
                var PlannedMoves = new Dictionary<T, Tuple<PartitionSet<T>, PartitionSet<T>>>();
                foreach (var set in sets)
                {
                    foreach (var el in set.Items)
                    {
                        //remove it first so it has a free choice
                        var targetSet = FindBestPartition(el, sets);
                        if (targetSet != set)
                        {
                            moveCt++;
                            var data = new Tuple<PartitionSet<T>, PartitionSet<T>>(set, targetSet);
                            PlannedMoves[el] = data;
                        }
                        if (moveCt > 0)
                        {
                            break;
                        }
                    }
                    if (moveCt > 0)
                    {
                        break;
                    }
                }

                //problem: I am moving to favor the article, not to favor the overall quality of matches.  i.e. if there is a linking article who is happier in a dedicated node, but removing him hurts the parent, how to do it?
                foreach (var article in PlannedMoves.Keys)
                {
                    var tup = PlannedMoves[article];
                    var old = tup.Item1;
                    var newset = tup.Item2;
                    old.Remove(article);
                    newset.Add(article);
                }
                loopCt++;
                stats[$"quality:{loopCt} moved:{moveCt}"] = FindQuality(sets);
                if (moveCt == 0)
                {
                    break;
                }
            }

            stats["moveCt"] = moveCt;
            stats["loopCt"] = loopCt;
            stats["Final quality"] = FindQuality(sets);

            var pdata = new PartitionData<T>(sets, stats);
            return pdata;

        }

        private static double FindQuality(List<PartitionSet<T>> sets)
        {
            return sets.Select(el => el.AverageDistance()).Sum() / sets.Count;
        }

        private static PartitionSet<T> FindBestPartition(T item, List<PartitionSet<T>> sets)
        {
            var bestSeen = double.MaxValue;
            PartitionSet<T> targetSet = null;
            foreach (var set in sets)
            {
                var candidate = set.Test(item);
                if (candidate < bestSeen)
                {
                    bestSeen = candidate;
                    targetSet = set;
                }
            }
            return targetSet;
        }
    }

    /// <summary>
    /// A List<T> with an average internal distance function.
    /// </summary>
    public class PartitionSet<T>
    {
        public List<T> Items;

        public DistanceMetrics<T> DistanceMetrics;
        public int Number;
        public PartitionSet(DistanceMetrics<T> dm, int number)
        {
            DistanceMetrics = dm;
            Items = new List<T>();
            Number = number;
        }

        public string ToString()
        {
            return $"PartitionSet:{Number} Items:{Items.Count} Dist:{AverageDistance()}";
        }

        public void Add(T element)
        {
            Items.Add(element);
            _LastDistance = double.NaN;
        }

        public void Remove(T element)
        {
            if (!Items.Contains(element))
            {
                //TODO: remove this once algo works
                throw new Exception("Item not present.");
            }

            Items.Remove(element);
            _LastDistance = double.NaN;
        }

        /// <summary>
        /// If we add element, what is the new "internal distance" of the group?
        /// Also we should exclude element.
        /// </summary>
        public double Test(T element)
        {
            if (Items.Count == 0)
            {
                return 0;
            }
            var result = 0.0;
            foreach (var el in Items)
            {
                if (el.Equals(element))
                {
                    continue;
                }
                result += DistanceMetrics.GetDistance(el, element);
            }

            return result * 1.0 / Items.Count;
        }

        private double _LastDistance = double.NaN;

        public double AverageDistance()
        {
            if (!double.IsNaN(_LastDistance))
            {
                return _LastDistance;
            }

            var result = 0.0;
            var ct = 0;
            foreach (var el in Items)
            {
                foreach (var el2 in Items)
                {
                    if (el.Equals(el2))
                    {
                        continue;
                    }
                    result += DistanceMetrics.GetDistance(el, el2);
                    ct++;
                }
            }

            if (Items.Count == 0 || Items.Count == 1)
            {
                return 0;
            }

            var res = result * 1.0 / ct;
            _LastDistance = res;
            return res;
        }
    }
}