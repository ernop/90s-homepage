using System;
using System.Collections.Generic;

namespace FusekiC
{

    public class PublishConfiguration
    {
        public PublishConfiguration(string tempBase, string cssSource, string jsSource, string imageSource, string publishTarget)
        {
            foreach (var el in new List<string>() { tempBase, cssSource, jsSource, imageSource })
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

            //validate all of these.
            var root = System.IO.Directory.GetCurrentDirectory();
            foreach (var dir in new List<string>() { tempBase, cssSource, jsSource, imageSource })
            {
                var target = System.IO.Path.Combine(root, dir);
                if (!System.IO.Directory.Exists(target))
                {
                    Console.WriteLine($"Target missing: {target}");
                    throw new Exception($"Missing directory {target}");
                }
                Console.WriteLine($"Found dir:{target}");
            }
        }

        public string TempBase { get; set; }
        public string CssSource { get; set; }
        public string JsSource { get; set; }
        public string ImageSource { get; set; }
        public string PublishTarget { get; set; }
    }
}
