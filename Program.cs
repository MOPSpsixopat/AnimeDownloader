using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Diagnostics;

class Program
{
    static readonly HttpClient client = new HttpClient(new HttpClientHandler
    {
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
    });

    static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
        client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");

        Console.Write("Link on episode: ");
        string baseEpisodeUrl = Console.ReadLine()?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(baseEpisodeUrl))
        {
            Console.WriteLine("Error: link is empty");
            return;
        }

        Console.Write("How many episodes would you like to download (enter - only this): ");
        string endInput = Console.ReadLine()?.Trim() ?? "";

        if (!int.TryParse(GetEpisodeNumber(baseEpisodeUrl), out int startEp))
        {
            Console.WriteLine("The episode number could not be determined");
            return;
        }

        int endEp = string.IsNullOrWhiteSpace(endInput) ? startEp : int.Parse(endInput);

        for (int i = startEp; i <= endEp; i++)
        {
            string currentUrl = ReplaceEpisodeNumber(baseEpisodeUrl, i);
            await DownloadEpisodeAsync(currentUrl, i);
        }

        Console.WriteLine("✅ The download is completed");
    }

    static string GetEpisodeNumber(string url)
    {
        var match = Regex.Match(url, @"episode-(\d+)");
        return match.Success ? match.Groups[1].Value : null!;
    }

    static string ReplaceEpisodeNumber(string url, int newEp)
    {
        return Regex.Replace(url, @"episode-(\d+)", $"episode-{newEp}");
    }

    static async Task DownloadEpisodeAsync(string episodeUrl, int episodeNumber)
    {
        Console.WriteLine($"\n🔍 Episode {episodeNumber}: {episodeUrl}");

        try
        {
            var response = await client.GetAsync(episodeUrl);
            response.EnsureSuccessStatusCode();
            
            var byteArray = await response.Content.ReadAsByteArrayAsync();
            string html;
            
            try
            {
                html = Encoding.UTF8.GetString(byteArray);
            }
            catch
            {
                html = Encoding.GetEncoding("windows-1251").GetString(byteArray);
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var videoNode = doc.DocumentNode.SelectSingleNode("//video");
            if (videoNode == null)
            {
                Console.WriteLine("⚠️ Video tag not found.");
                return;
            }

            var sourceTags = videoNode.SelectNodes(".//source[@type='video/mp4']");
            if (sourceTags == null || sourceTags.Count == 0)
            {
                Console.WriteLine("⚠️ No MP4 sources found.");
                return;
            }

            var bestSource = sourceTags
                .Select(node => new
                {
                    Url = WebUtility.HtmlDecode(node.GetAttributeValue("src", "")),
                    Quality = int.TryParse(node.GetAttributeValue("res", ""), out int q) ? q : 0
                })
                .OrderByDescending(v => v.Quality)
                .FirstOrDefault();

            if (bestSource == null || string.IsNullOrEmpty(bestSource.Url))
            {
                Console.WriteLine("⚠️ No valid video source found.");
                return;
            }

            Uri videoUri;
            if (!Uri.TryCreate(bestSource.Url, UriKind.Absolute, out _))
            {
                var baseUri = new Uri(episodeUrl);
                videoUri = new Uri(baseUri, bestSource.Url);
            }
            else
            {
                videoUri = new Uri(bestSource.Url);
            }

            string filename = $"episode-{episodeNumber}.{bestSource.Quality}p.mp4";
            Console.WriteLine($"📥 Downloading: {videoUri}");

            await DownloadFileWithProgressAsync(videoUri.ToString(), filename, episodeNumber);
            
            Console.WriteLine($"\n✅ Downloaded: {filename}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error processing episode {episodeNumber}: {ex.Message}");
        }
    }

    static async Task DownloadFileWithProgressAsync(string fileUrl, string fileName, int episodeNumber)
    {
        try
        {
            using var response = await client.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            long? totalBytes = response.Content.Headers.ContentLength;
            if (!totalBytes.HasValue)
            {
                Console.WriteLine("❌ Cannot determine file size.");
                return;
            }

            var total = totalBytes.Value;
            var buffer = new byte[8192];
            long downloaded = 0;
            var sw = Stopwatch.StartNew();

            using var stream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            Console.Write(new string(' ', Console.WindowWidth - 1));
            Console.SetCursorPosition(0, Console.CursorTop);

            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer);
                if (bytesRead == 0) break;

                await fileStream.WriteAsync(buffer, 0, bytesRead);
                downloaded += bytesRead;

                UpdateProgress(episodeNumber, downloaded, total, sw.Elapsed.TotalSeconds);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Download error: {ex.Message}");
            if (File.Exists(fileName))
                File.Delete(fileName);
            throw;
        }
    }

    static void UpdateProgress(int episodeNumber, long downloaded, long total, double elapsedSeconds)
{
    double progress = Math.Min((double)downloaded / total, 1.0);
    double percent = progress * 100;
    double speed = downloaded / elapsedSeconds;
    
    string speedStr;
    if (speed > 1024 * 1024)
        speedStr = $"{speed / (1024 * 1024):0.00} MB/s";
    else if (speed > 1024)
        speedStr = $"{speed / 1024:0.00} KB/s";
    else
        speedStr = $"{speed:0.00} B/s";

    string bar = new string('=', (int)(progress * 20)).PadRight(20);
    string line = $"Episode {episodeNumber:D3}: [{bar}] {percent,5:0.0}% " +
                  $"{downloaded / (1024 * 1024):0.0} MB / {total / (1024 * 1024):0.0} MB " +
                  $"at {speedStr}";

    Console.SetCursorPosition(0, Console.CursorTop);
    Console.Write(line.PadRight(Console.WindowWidth - 1));
    Console.Out.Flush();
}
}