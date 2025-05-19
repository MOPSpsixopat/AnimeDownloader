using AnimeDownloader.Services;
using System.Net;
using System.Text;

class Program
{
    static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        var httpClient = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        });

        ConfigureHttpClient(httpClient);

        Console.Write("Link on episode: ");
        string baseUrl = Console.ReadLine()?.Trim() ?? "";

        if (string.IsNullOrEmpty(baseUrl))
        {
            Console.WriteLine("Error: URL is empty.");
            return;
        }

        Console.Write("How many episodes to download (enter for 1): ");
        int endEp = int.TryParse(Console.ReadLine(), out int ep) ? ep : 1;

        var parser = new ParserService(httpClient);
        var downloader = new DownloadService(httpClient);
        var episodeService = new EpisodeService();

        int startEp = ParserService.ExtractEpisodeNumber(baseUrl);
        if (startEp == 0)
        {
            Console.WriteLine("Cannot parse episode number.");
            return;
        }

        var episodeUrls = episodeService.GenerateEpisodeUrls(baseUrl, startEp, startEp + endEp - 1);

        foreach (var url in episodeUrls)
        {
            var episode = await parser.ParseEpisodeAsync(url);
            if (episode != null)
            {
                await downloader.DownloadEpisodeAsync(episode);
            }
            else
            {
                Console.WriteLine($"⚠️ Failed to parse episode: {url}");
            }
        }

        Console.WriteLine("✅ All episodes downloaded!");
    }

    static void ConfigureHttpClient(HttpClient httpClient)
    {
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        httpClient.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
        httpClient.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br");
        httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
    }
}