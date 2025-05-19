namespace AnimeDownloader.Utilities;

public static class ConsoleHelper
{
    public static void UpdateProgress(int episodeNumber, long downloaded, long total, double elapsedSeconds)
    {
        double progress = Math.Min((double)downloaded / total, 1.0);
        double percent = progress * 100;
        double speed = downloaded / elapsedSeconds;

        string speedStr = speed switch
        {
            > 1024 * 1024 => $"{speed / (1024 * 1024):0.00} MB/s",
            > 1024 => $"{speed / 1024:0.00} KB/s",
            _ => $"{speed:0.00} B/s"
        };

        string bar = new string('=', (int)(progress * 20)).PadRight(20);
        string line = $"Episode {episodeNumber:D3}: [{bar}] {percent,5:0.0}% " +
                      $"{downloaded / (1024 * 1024):0.0} MB / {total / (1024 * 1024):0.0} MB " +
                      $"at {speedStr}";

        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(line.PadRight(Console.WindowWidth - 1));
        Console.Out.Flush();
    }
}