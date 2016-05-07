using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.OptionsModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
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
        public string ImdbId { get; internal set; }
        public string Name { get; set; }
        public IEnumerable<SeasonViewModel> Seasons { get; set; }
        public string ThumbnailUrl { get; internal set; }
        public string Rating { get; internal set; }
        public string VoteCount { get; internal set; }
        public string Plot { get; set; }
    }
    public class ShowController : Controller
    {
        AppSettings Options { get; set; }
        public ShowController(IOptions<AppSettings> optionsAccessor)
        {
            Options = optionsAccessor.Value;
        }
        public static SearchTvResult GetApiInfo(string showName)
        {
            showName = showName.ToLower();
            if (!ApiResults.ContainsKey(showName))
            {
                lock ("toto")
                {
                    using (var client = new HttpClient { BaseAddress = new Uri("http://www.omdbapi.com/") })
                    {
                        using (var response = client.GetAsync(FormatUrl("", new { t = showName, type = "series", plot = "short" })).Result)
                        {
                            var str = response.Content.ReadAsStringAsync().Result;
                            System.Diagnostics.Debug.WriteLine(str);
                            var r = JsonConvert.DeserializeObject<SearchTvResult>(str);
                            //var name = response.Data.;
                            ApiResults.Add(showName, r);
                        }

                    }
                }
            }
            var api = ApiResults[showName];
            return api ?? new SearchTvResult();
        }


        private Regex _regex = new Regex("(?<name>[a-zA-Z.0-9]*).[Ss](?<season>[0-9]{2})[Ee](?<episode>[0-9]{2}).(?<quality>[a-zA-Z0-9]*).(.*).(?<extension>mkv|avi)");
        private IDictionary<string, Match> Files => Directory.GetFiles(Options.FolderPath).ToDictionary(x => x, x => _regex.Match(x));

        public IActionResult Debug()
        {
            return Json(Files.Select(x => new { x.Key, x.Value.Success}));
        }

        public IActionResult Index(string query)
        {

            var shows = Files.Values.Where(m => m.Success).Select(x =>
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
            .ToList()
            .GroupBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).Select(x => new ShowViewModel
            {
                Name = x.Key,
                ThumbnailUrl = GetApiInfo(x.Key).Poster,
                ImdbId = GetApiInfo(x.Key).imdbID,
                Rating = GetApiInfo(x.Key).imdbRating,
                VoteCount = GetApiInfo(x.Key).imdbVotes,

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

            return View(shows);
        }

        static string FormatUrl(string path, IDictionary<string, object> parameters)
        {
            if (parameters == null)
                parameters = new Dictionary<string, object>();

            //parameters.Add("api_key", "0f7de12810d35bd62dd0e93978f39cae");

            return $"{path}?{string.Join("&", parameters.Select(x => $"{x.Key}={x.Value}"))}";
        }
        static string FormatUrl(string path1, string path2, string path3, object parameters = null)
        {
            return FormatUrl($"{path1}/{path2}/{path3}", parameters);
        }
        static string FormatUrl(string path1, string path2, object parameters = null)
        {
            return FormatUrl($"{path1}/{path2}", parameters);
        }
        static string FormatUrl(string path, object parameters = null)
        {
            return FormatUrl(path, parameters?.GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
                .ToDictionary
                (
                    propInfo => propInfo.Name,
                    propInfo => propInfo.GetValue(parameters, null)
                ));
        }


        private const string ImageRootUrl = "http://image.tmdb.org/t/p/w92";
        private static IDictionary<string, SearchTvResult> ApiResults = new Dictionary<string, SearchTvResult>();

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

    public class SearchTvResult
    {
        public string Title { get; set; }
        public string Year { get; set; }
        public string Rated { get; set; }
        public string Released { get; set; }
        public string Runtime { get; set; }
        public string Genre { get; set; }
        public string Director { get; set; }
        public string Writer { get; set; }
        public string Actors { get; set; }
        public string Plot { get; set; }
        public string Language { get; set; }
        public string Country { get; set; }
        public string Awards { get; set; }
        public string Poster { get; set; }
        public string Metascore { get; set; }
        public string imdbRating { get; set; }
        public string imdbVotes { get; set; }
        public string imdbID { get; set; }
        public string Type { get; set; }
        public string Response { get; set; }
    }
}
