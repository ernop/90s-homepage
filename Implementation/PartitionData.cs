using System.Collections.Generic;

namespace FusekiC
{
    public class PartitionData<T>
    {
        public List<PartitionSet<T>> PSets
        {
            get; set;
        }
        public Dictionary<string, object > Data { get; set; }
        public PartitionData(List<PartitionSet<T>> psets, Dictionary<string, object> data)
        {
            PSets = psets;
            Data = data;
        }
    }
}
