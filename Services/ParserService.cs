using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using AnimeDownloader.Model;

namespace AnimeDownloader.Services;

public class ParserService
{
    private readonly HttpClient _httpClient;

    public ParserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<EpisodeInfo?> ParseEpisodeAsync(string episodeUrl)
    {
        try
        {
            var response = await _httpClient.GetAsync(episodeUrl);
            response.EnsureSuccessStatusCode();

            var byteArray = await response.Content.ReadAsByteArrayAsync();
            string html = Encoding.UTF8.GetString(byteArray);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var videoNode = doc.DocumentNode.SelectSingleNode("//video");
            if (videoNode == null) return null;

            var sourceTags = videoNode.SelectNodes(".//source[@type='video/mp4']");
            if (sourceTags == null || sourceTags.Count == 0) return null;

            var bestSource = sourceTags
                .Select(node => new
                {
                    Url = WebUtility.HtmlDecode(node.GetAttributeValue("src", "")),
                    Quality = int.TryParse(node.GetAttributeValue("res", ""), out int q) ? q : 0
                })
                .OrderByDescending(v => v.Quality)
                .FirstOrDefault();

            if (bestSource == null || string.IsNullOrEmpty(bestSource.Url)) return null;

            Uri videoUri = Uri.TryCreate(bestSource.Url, UriKind.Absolute, out _) 
                ? new Uri(bestSource.Url) 
                : new Uri(new Uri(episodeUrl), bestSource.Url);

            return new EpisodeInfo
            {
                Number = ExtractEpisodeNumber(episodeUrl),
                Url = episodeUrl,
                DownloadUrl = videoUri.ToString(),
                Quality = bestSource.Quality
            };
        }
        catch
        {
            return null;
        }
    }

    public static int ExtractEpisodeNumber(string url) 
    {
        var match = Regex.Match(url, @"episode-(\d+)");
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }
}