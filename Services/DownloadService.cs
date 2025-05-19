using AnimeDownloader.Model;
using System.Diagnostics;

namespace AnimeDownloader.Services;

public class DownloadService
{
    private readonly HttpClient _httpClient;

    public DownloadService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task DownloadEpisodeAsync(EpisodeInfo episode)
    {
        try
        {
            Console.WriteLine($"📥 Downloading: {episode.DownloadUrl}");
            await DownloadFileWithProgressAsync(episode.DownloadUrl, episode.FileName, episode.Number);
            Console.WriteLine($"\n✅ Downloaded: {episode.FileName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error downloading episode {episode.Number}: {ex.Message}");
            if (File.Exists(episode.FileName))
                File.Delete(episode.FileName);
        }
    }

    private async Task DownloadFileWithProgressAsync(string fileUrl, string fileName, int episodeNumber)
    {
        using var response = await _httpClient.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        long totalBytes = response.Content.Headers.ContentLength ?? 0;
        if (totalBytes == 0) throw new Exception("Cannot determine file size.");

        using var stream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write);

        var buffer = new byte[8192];
        long downloaded = 0;
        var sw = Stopwatch.StartNew();

        while (true)
        {
            int bytesRead = await stream.ReadAsync(buffer);
            if (bytesRead == 0) break;

            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
            downloaded += bytesRead;

            Utilities.ConsoleHelper.UpdateProgress(episodeNumber, downloaded, totalBytes, sw.Elapsed.TotalSeconds);
        }
    }
}