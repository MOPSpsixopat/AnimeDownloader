using System.Text.RegularExpressions;

namespace AnimeDownloader.Services;

public class EpisodeService
{
    public IEnumerable<string> GenerateEpisodeUrls(string baseUrl, int startEp, int endEp)
    {
        for (int i = startEp; i <= endEp; i++)
        {
            yield return ReplaceEpisodeNumber(baseUrl, i);
        }
    }

    private string ReplaceEpisodeNumber(string url, int newEp)
    {
        return Regex.Replace(url, @"episode-(\d+)", $"episode-{newEp}");
    }
}