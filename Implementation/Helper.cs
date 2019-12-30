using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FusekiC
{
    public static class Helpers
    {
        public static Regex TitleRegex = new Regex(@"([\w ]+)");
        public static Regex InternalLinkRegex = new Regex(@"\[([\w ]+)\]");
        public static MetatagListModel GetMetatagListModel()
        {
            var metatags = new List<Metatag>();

            var m = new MetatagListModel("All", metatags);

            using (var db = new FusekiContext())
            {
                var allTags = db.Tags.Select(el => el.Name).ToHashSet();
                foreach (var tag in allTags)
                {
                    var mt = new Metatag();
                    mt.Name = tag;
                    var articleIds = db.Tags.Where(el => el.Name == tag).Select(el => el.ArticleId);
                    var articles = db.Articles.Where(el => articleIds.Contains(el.Id) && el.Published).ToList(); ;
                    if (articles.Count == 0)
                    {
                        continue;
                    }
                    mt.Articles = articles;
                    mt.Count = articles.Count;
                    mt.Created = articles.OrderBy(el => el.Created).FirstOrDefault()?.Created ?? DateTime.MinValue;
                    mt.Updated = articles.OrderByDescending(el => el.Updated).FirstOrDefault()?.Updated ?? DateTime.MinValue;
                    metatags.Add(mt);
                }
            }
            m.Metatags = metatags.OrderByDescending(el => el.Count).ThenBy(el=>el.Name).ToList();
            return m;
        }

        public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }
}