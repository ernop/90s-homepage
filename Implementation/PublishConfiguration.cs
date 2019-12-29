using System.Collections.Generic;

namespace FusekiC
{

    public class PublishConfiguration
    {
        public PublishConfiguration(string tempBase, string cssSource, string jsSource, string imageSource, string publishTarget)
        {
            foreach (var el in new List<string>() { tempBase, cssSource, jsSource, imageSource, publishTarget })
            {
                if (el.EndsWith("/"))
                {
                    throw new System.Exception("invalid publishconfiguration");
                }
            }

            TempBase = tempBase;
            CssSource = cssSource;
            JsSource = jsSource;
            ImageSource = imageSource;
            PublishTarget = publishTarget;
        }
        public string TempBase { get; set; }
        public string CssSource { get; set; }
        public string JsSource { get; set; }
        public string ImageSource { get; set; }
        public string PublishTarget { get; set; }
    }
}
