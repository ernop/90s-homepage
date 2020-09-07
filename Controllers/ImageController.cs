using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Web;
using System.Text.RegularExpressions;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace FusekiC
{
    [Authorize]

    public class ImageController : Controller
    {
        private static Regex AlphaNumeric = new Regex("^[a-zA-Z0-9]*$");

        private PublishConfiguration PublishConfiguration { get; set; }
        public ImageController(PublishConfiguration pc)
        {
            PublishConfiguration = pc;
        }


        [HttpPost("/image/delete")]
        public IActionResult Delete(int id)
        {
            using (var db = new FusekiContext())
            {
                var image = db.Images
                    .Include(el => el.ImageTags)
                    .FirstOrDefault(el => el.Id == id);
                if (image != null)
                {
                    image.Deleted = true;
                }
                db.SaveChanges();
            }
            return RedirectToAction("ViewImages");
        }

        [HttpPost("/image/settags")]
        public IActionResult SetTags(int id, string tagLine = "")
        {
            if (string.IsNullOrEmpty(tagLine))
            {
                tagLine = "";
            }
            var newTags = Helpers.TagLine2Tags(tagLine);
            var didSomething = false;
            var result = "";
            using (var db = new FusekiContext())
            {
                var image = db.Images
                     .Include(el => el.ImageTags)
                     .FirstOrDefault(el => el.Id == id);
                var exiTags = image.ImageTags ?? new List<ImageTag>();
                //exiTags = new List<ImageTag>();
                var toDelete = new List<ImageTag>();
                foreach (var tag in newTags)
                {
                    if (exiTags.FirstOrDefault(el => el.Name == tag) == null)
                    {
                        var newImageTag = new ImageTag();
                        newImageTag.Image = image;
                        newImageTag.Name = tag;
                        db.Add(newImageTag);
                        didSomething = true;
                        result += $"\ncreated new tag for image: {tag}";
                    }
                }

                foreach (var exiTag in exiTags)
                {
                    if (newTags.FirstOrDefault(el => el == exiTag.Name) == null)
                    {
                        db.Remove(exiTag);
                        didSomething = true;
                        result += $"\nremoved old tag on image: {exiTag.Name}";
                    }
                }
                if (didSomething) {
                    db.SaveChanges();
                }
            }

            var arm = new ActionResultModel();
            arm.NextLink = "/image/viewall";
            arm.NextLinkDescription = "back to image list";
            arm.SetResult(result);

            return View("ActionResult", arm);

        }



        //todo fulldelete

        [HttpGet("/image/upload")]
        public IActionResult UploadGet()
        {

            

            return View("Upload");
        }


        [HttpGet("/image/upload2")]
        public IActionResult Upload2Get()
        {
            return View("Upload2");
        }

        private static bool ValidateImageExtension(string ext)
        {
            var Valids = new List<string>() { ".png", ".jpg", ".gif",".jfif" };
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
        public IActionResult UploadPost(UploadFileModel file, string tagLine)
        {
            //possibly accept the override filename
            
            var filename = file.Image.FileName;
            if (string.IsNullOrEmpty(file.Filename))
            {
                file.Filename = file.Filename.ToLower();
            }
            else
            {
                filename = file.Filename;
            }

            filename = CleanFilename(filename);

            if (!ValidateImageExtension(filename))
            {
                var extFromImage = Path.GetExtension(file.Image.FileName);

                if (ValidateImageExtension(extFromImage))
                {

                    filename = filename + extFromImage;

                }
                else
                {
                    throw new Exception("Invalid extension on filename" + filename);
                }
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

            using (var db = new FusekiContext())
            {
                var image = new Image();
                image.Filename = filename;
                db.Add(image);
                var tags = Helpers.TagLine2Tags(tagLine);
                foreach (var tag in tags)
                {
                    var t = new ImageTag();
                    t.Image = image;
                    t.Name = tag;
                    db.Add(t);
                }
                db.SaveChanges();
            }

            var arm = new ActionResultModel();
            arm.NextLink = "/image/upload";
            arm.NextLinkDescription = "Return to image upload";
            arm.SetResult("Uploaded image");
            return View("ActionResult", arm);
        }

        /// <summary>
        /// reload images from directory
        /// </summary>
        [HttpGet("image/restore")]
        public IActionResult RestoreImages()
        {
            var images = System.IO.Directory.GetFiles(PublishConfiguration.ImageSource).Select(el => el.Replace(PublishConfiguration.ImageSource, "").Replace("\\", ""));
            using (var db = new FusekiContext())
            {
                foreach (var imagefn in images)
                {
                    var exi = db.Images.FirstOrDefault(el => el.Filename == imagefn);
                    if (exi == null)
                    {
                        var image = new Image();
                        image.Filename = imagefn;
                        db.Add(image);
                    }
                }
                db.SaveChanges();
            }

            return null;
        }

        [HttpGet("/image/viettag")]
        public IActionResult ViewImageTag(string term)
        {
            using (var db = new FusekiContext())
            {
                var model = new ViewImagesModel();
                model.Term = term;
                term = term.ToLower();
                var images = new List<Image>();
                string matchType = "";
                var tag = db.ImageTags.FirstOrDefault(el => el.Name.ToLower() == term);
                if (tag == null)
                {
                    tag = db.ImageTags.FirstOrDefault(el => el.Name.ToLower().StartsWith(term));
                    matchType = "prefix";
                }
                else if (tag == null)
                {
                    tag = db.ImageTags.FirstOrDefault(el => el.Name.ToLower().Contains(term));
                    matchType = "substring";
                }
                else if (tag == null)
                {
                    tag = null;
                    matchType = "none";
                }
                else
                {
                    matchType = "exact";
                }

                if (tag != null)
                {

                    var tagName = tag.Name;
                    var tags = db.ImageTags.Where(el => el.Name == tagName);
                    images = tags.Select(el => el.Image)
                        .Include(el => el.ImageTags)
                        .ToList();
                }

                model.Images = images;
                model.MatchType = matchType;
                return View("ViewImages", model);
            }
        }

        [HttpGet("image/viewall")]
        public IActionResult ViewImages(string term)
        {
            using (var db = new FusekiContext())
            {
                var model = new ViewImagesModel();
                model.Term = term;
                IOrderedQueryable<Image> images;
                if (!string.IsNullOrEmpty(term))
                {
                    images = db.Images.Where(el => el.Filename.ToLower().Contains(term.ToLower())).OrderByDescending(el => el.Id);
                    model.Term = "Search for: " + term;
                }
                else
                {
                    images = db.Images.OrderByDescending(el => el.Id).Take(20).OrderByDescending(el => el.Id);
                }

                model.Images = images
                    .Where(el => el.Deleted == false)
                    .Include(el => el.ImageTags)
                    .ToList();
                return View("ViewImages", model);
            }
        }

        [HttpPost("/image/search")]
        public IActionResult SearchImages(string term)
        {
            return RedirectToAction("ViewImages", new { term });
        }

        //Todo this probably needs fixing
        private static string CleanFilename(string n)
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
            return res.ToLower();
        }
    }


}
