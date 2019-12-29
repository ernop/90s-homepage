using System.Collections.Generic;

namespace FusekiC
{

    public class PublicationListModel
    {
        public PublicationListModel() { }
        public PublicationListModel(List<Publication> pubs)
        {
            Publications = pubs;
        }
        public List<Publication> Publications { get; set; }
    }
}
