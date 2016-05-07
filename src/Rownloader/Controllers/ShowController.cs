using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.OptionsModel;
using RestSharp;
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

        //public FileInfo FileInfo =>  new FileInfo(Path.Combine(ShowController.FolderPath,Filename));
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
        AppSettings Options { get; set; }
        public ShowController(IOptions<AppSettings> optionsAccessor)
        {
            Options = optionsAccessor.Value;
        }
        public IActionResult Index(string query)
        {
            var regex = new Regex("(?<name>[a-zA-Z.0-9]*).[Ss](?<season>[0-9]{2})[Ee](?<episode>[0-9]{2}).(?<quality>[a-zA-Z0-9]*).(.*).(?<extension>mkv|avi)");
            var files = Directory.GetFiles(Options.FolderPath).Select(x => regex.Match(x)).Where(m => m.Success).Select(x =>
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

        public ActionResult Thumbnail(string showName)
        {
            var client = new RestClient("http://api.themoviedb.org/3/");
            var request = new RestRequest("search/tv");
            request.AddParameter("api_key", "0f7de12810d35bd62dd0e93978f39cae");
            request.AddParameter("query", showName);


            var response = client.Execute<ApiResult<Result>>(request);
            //var name = response.Data.;

            return Redirect("http://image.tmdb.org/t/p/w92" + response.Data.results.First().poster_path);

        }

        public void Download(string filename)
        {
            var path = Path.Combine(Options.FolderPath, filename);
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

    public class Result
    {
        public string poster_path { get; set; }
        public double popularity { get; set; }
        public int id { get; set; }
        public string backdrop_path { get; set; }
        public double vote_average { get; set; }
        public string overview { get; set; }
        public string first_air_date { get; set; }
        public List<object> origin_country { get; set; }
        public List<object> genre_ids { get; set; }
        public string original_language { get; set; }
        public int vote_count { get; set; }
        public string name { get; set; }
        public string original_name { get; set; }
    }

    public class ApiResult<T>
    {
        public int page { get; set; }
        public List<T> results { get; set; }
        public int total_results { get; set; }
        public int total_pages { get; set; }
    }

}
