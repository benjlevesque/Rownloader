using Microsoft.AspNet.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Rownloader.Controllers
{
    public class EpisodeViewModel
    {
        public string Name { get; set; }
        public int Season { get; set; }
        public int Episode { get; set; }
        public string Quality { get; set; }
        public string Extension { get; set; }
        public string Filename { get; set; }
    }
    public class SeasonViewModel
    {
        public int Season { get; set; }
        public IEnumerable<EpisodeViewModel> Episodes { get; set; }
    }
    public class ShowViewModel
    {
        public string Name { get; set; }
        public IEnumerable<SeasonViewModel> Seasons { get; set; }
    }
    public class ShowController : Controller
    {
        public const string FolderPath = @"C:\Rownloader";
        public IActionResult Index(string query)
        {
            var regex = new Regex("(?<name>[a-zA-Z.0-9]*).[Ss](?<season>[0-9]{2})[Ee](?<episode>[0-9]{2}).(?<quality>[a-zA-Z0-9]*).(.*).(?<extension>mkv|avi)");
            var files = Directory.GetFiles(FolderPath).Select(x => regex.Match(x)).Where(m => m.Success).Select(x =>
            new
            {
                Filename = x.Value,
                Name = x.Groups["name"].Value.Replace('.', ' '),
                Episode = int.Parse(x.Groups["episode"].Value),
                Season = int.Parse(x.Groups["season"].Value),
                Quality = x.Groups["quality"].Value,
                Extension = x.Groups["extension"].Value
            })
            .Where(x => query == null || x.Filename.ToLower().Contains(query.ToLower()))
            .GroupBy(x => x.Name).Select(x => new ShowViewModel
            {
                Name = x.Key,
                Seasons = x.GroupBy(y => y.Season).Select(y => new SeasonViewModel
                {
                    Season = y.Key,
                    Episodes = y.Select(z => new EpisodeViewModel
                    {
                        Episode = z.Episode,
                        Filename = z.Filename,
                        Quality = z.Quality,
                        Extension = z.Extension,
                        Name = z.Name,
                        Season = z.Season
                    }).OrderBy(z => z.Episode)
                }).OrderBy(y => y.Season)
            }).OrderBy(x => x.Name);



            return View(files);
        }

        public void Download(string filename)
        {
            var path = Path.Combine(FolderPath, filename);
            if (!System.IO.File.Exists(path))
            {
                return;
            }

            Response.Headers.Add("content-disposition", $"filename={filename}");


            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                //Response.BufferOutput = false;   // to prevent buffering
                byte[] buffer = new byte[1024];
                int bytesRead = 0;
                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    Response.Body.Write(buffer, 0, bytesRead);
                }
            }

        }
    }
}
