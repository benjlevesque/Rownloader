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
        public double VoteAverage { get; internal set; }
        public int VoteCount { get; internal set; }
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
                    using (var client = new HttpClient { BaseAddress = new Uri("http://api.themoviedb.org/3/") })
                    {
                        SearchTvResult r;
                        using (var response = client.GetAsync(FormatUrl("search/tv", new { query = showName })).Result)
                        {
                            var str = response.Content.ReadAsStringAsync().Result;
                            var data = JsonConvert.DeserializeObject<ApiResult<SearchTvResult>>(str);
                            //var name = response.Data.;
                            r = data.results.FirstOrDefault();
                        }
                        if (r != null)
                        {
                            var url = FormatUrl($"tv/{r.id}/external_ids");
                            using (var response = client.GetAsync(url).Result)
                            {
                                var str = response.Content.ReadAsStringAsync().Result;
                                var data = JsonConvert.DeserializeObject<ExternalIdsResults>(str);
                                Console.WriteLine(data);

                                //var name = response.Data.;
                                r.ImdbId = data.imdb_id;
                            }
                        }
                        ApiResults.Add(showName, r);

                    }
                }
            }
            var api = ApiResults[showName];
            return api ?? new SearchTvResult();
        }
        public IActionResult Index(string query)
        {
            var regex = new Regex("(?<name>[a-zA-Z.0-9]*).[Ss](?<season>[0-9]{2})[Ee](?<episode>[0-9]{2}).(?<quality>[a-zA-Z0-9]*).(.*).(?<extension>mkv|avi)");
            var shows = Directory.GetFiles(Options.FolderPath).Select(x => regex.Match(x)).Where(m => m.Success).Select(x =>
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
            .GroupBy(x => x.Name).Select(x => new ShowViewModel
            {
                Name = x.Key,
                ThumbnailUrl = ImageRootUrl + GetApiInfo(x.Key).poster_path,
                ImdbId = GetApiInfo(x.Key).ImdbId,
                VoteAverage = GetApiInfo(x.Key).vote_average,
                VoteCount = GetApiInfo(x.Key).vote_count,
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

            parameters.Add("api_key", "0f7de12810d35bd62dd0e93978f39cae");

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
        public string ImdbId { get; internal set; }
    }

    public class ExternalIdsResults
    {
        public string imdb_id { get; set; }
        public string freebase_mid { get; set; }
        public string freebase_id { get; set; }
        public string tvdb_id { get; set; }
        public string tvrage_id { get; set; }
        public int id { get; set; }
    }

    public class ApiResult<T>
    {
        public int page { get; set; }
        public List<T> results { get; set; }
        public int total_results { get; set; }
        public int total_pages { get; set; }
    }

}
