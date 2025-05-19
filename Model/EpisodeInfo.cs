namespace AnimeDownloader.Model;

public class EpisodeInfo
{
    public int Number { get; set; }
    public string Url { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public int Quality { get; set; }
    public string FileName => $"episode-{Number}.{Quality}p.mp4";
}