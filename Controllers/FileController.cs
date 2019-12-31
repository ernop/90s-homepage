using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Web;
using System.Text.RegularExpressions;
using System.IO;

namespace FusekiC
{
    [Authorize]
    
    public class FileController : Controller
    {
        private static Regex AlphaNumeric = new Regex("^[a-zA-Z0-9]*$");
        
        private PublishConfiguration PublishConfiguration { get; set; }
        public FileController(PublishConfiguration pc)
        {
            PublishConfiguration = pc;
        }
        
        
        [HttpGet("/image/upload")]
        public IActionResult UploadGet()
        {
            return View("Upload");
        }

       private bool CheckExtension(string ext) {
            var Valids = new List<string>() { ".png", ".jpg", ".gif" };
            foreach (var valid in Valids)
            {
                if (ext.EndsWith(valid))
                {
                    return true;
                }
            }
            return false;
        }

        [HttpPost("/image/upload")]
        public IActionResult UploadPost(UploadFileModel file)
        {
            var filename = file.Image.FileName;
            if (!string.IsNullOrEmpty(file.Filename))
            {
                filename = file.Filename;
            }

            filename = CleanFilename(filename);


            if (!CheckExtension(filename))
            {
                var extFromImage = Path.GetExtension(file.Image.FileName);

                if (CheckExtension(extFromImage))
                {

                    filename = filename + extFromImage;

                }
                else
                {
                    throw new Exception("Invalid extension on filename" + filename);
                }
            }
            else
            {
                throw new Exception("Invalid extension on filename" + filename);
            }

            var targetPath = $"{PublishConfiguration.ImageSource}/{filename}";
            if (System.IO.File.Exists(targetPath))
            {
                throw new System.Exception("Already exists.");
            }

            using (var stream = System.IO.File.Create(targetPath))
            {
                file.Image.CopyTo(stream);
            }

            return View("Upload");
        }

        [HttpGet("image/viewall")]
        public IActionResult ViewImages(string term)
        {
            var images = System.IO.Directory.GetFiles(PublishConfiguration.ImageSource).Select(el=>el.Replace(PublishConfiguration.ImageSource,"").Replace("\\",""));
            var model = new ViewImagesModel();
            model.Term = term;
            if (!string.IsNullOrEmpty(term))
            {
                images = images.Where(el => el.ToLower().Contains(term.ToLower())).ToArray();
                model.Term = "Search for: " + term;
            }
                        
            model.Images = images.Select(el => new ImageModel(el)).ToList();
            return View("ViewImages", model);
        }
        
        [HttpPost("/image/search")]
        public IActionResult SearchImages(string term)
        {
            return RedirectToAction("ViewImages", new { term });
        }

        private string CleanFilename(string n)
        {
            var res = "";
            var dotcount = 0;
            foreach (var c in n)
            {
                if (c == '.' && dotcount == 0)
                {
                    dotcount++;
                    res += c;
                    continue;
                }
                if (AlphaNumeric.IsMatch(c.ToString()))
                {
                    res += c;
                }
            }
            return res;
        }
    }

    public class ImageModel
    {
        public ImageModel(string fn)
        {
            Filename = fn;
        }
        public string Filename { get; set; }
    }

    public class ViewImagesModel
    {
        public string Term { get; set; }
        public List<ImageModel> Images {get;set;}
    }

    public class UploadFileModel
    {
        public string Filename { get; set; }
        public IFormFile Image { set; get; }
    }

    
}
