using Microsoft.AspNetCore.Http;

namespace FusekiC
{
    public class UploadFileModel
    {
        public string Filename { get; set; }
        public string TagLine{ get; set; }
        public IFormFile Image { set; get; }
    }


}
