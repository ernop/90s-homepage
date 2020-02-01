using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FusekiC
{
    public class Settings
    {
        public string PublishTarget { get; set; }

        /// <summary>
        /// Title for the header image
        /// </summary>
        public string SiteName { get; set; }
        
        /// <summary>
        /// google analytics id.
        /// </summary>
        public string GaId { get; set; }

        public string CookieScheme { get; set; }
        public string LiveUrlTemplate { get; set; }
        public string EditUrlTemplate { get; set; }
        public string Version { get; set; }
    }
}
