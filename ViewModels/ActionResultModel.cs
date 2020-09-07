using System.Runtime.InteropServices.ComTypes;

namespace FusekiC
{
    public class ActionResultModel
    {
        public string NextLink { get; set; }
        private string _Result = "Unset";
        
        public string NextLinkDescription { get; set; }

        public void SetResult(string result)
        {
            _Result = result.Replace("\n", "<br />");
        }

        public string GetResult()
        {
            return _Result;
        }
    }
}