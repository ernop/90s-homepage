using System;
using System.Collections.Generic;
using System.Linq;

namespace FusekiC
{
    public class ArticlePartitionModel
    {
        public ArticlePartitionModel(PartitionData<Article> partitionData)
        {
            Partitions = partitionData.PSets;
            Data = partitionData.Data;

            //tag list is an ordered list of tags by frequency appearing in all the articles.
            PrepareTagLists();
        }

        public Dictionary<string,object> Data { get; set; }

        public List<PartitionSet<Article>> Partitions { get; set; }
        public Dictionary<PartitionSet<Article>,List<string>> TagLists { get; set; }

        /// <summary>
        /// Taglist is tag => count for each pset so that we can make a nice jagged table
        /// </summary>
        private void PrepareTagLists()
        {
            TagLists = new Dictionary<PartitionSet<Article>, List<string>>();
            foreach (var pset in Partitions)
            {
                var tags = new Dictionary<string, int>();
                foreach (var article in pset.Items)
                {
                    foreach (var tag in article.Tags)
                    {
                        if (!tags.ContainsKey(tag.Name))
                        {
                            tags[tag.Name] = 0;
                        }
                        tags[tag.Name]++;
                    }
                }

                var res = new List<string>();
                foreach (var kvp in tags.OrderByDescending(el=>el.Value))
                {
                    res.Add(kvp.Key);
                }

                TagLists[pset] = res;
            }
        }
    }
}