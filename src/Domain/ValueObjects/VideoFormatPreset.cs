namespace VideoSaaS.Domain.ValueObjects;

public sealed record VideoFormatPreset(string Code, string Label, int Width, int Height, string FinalFileName)
{
    public string AspectRatioPrompt => Code.Contains("9_16", StringComparison.OrdinalIgnoreCase)
        ? "vertical 9:16"
        : "horizontal 16:9";

    public static VideoFormatPreset FromCode(string? code)
    {
        return code?.Trim().ToLowerInvariant() switch
        {
            "youtube_16_9" => new("youtube_16_9", "YouTube 16:9", 1920, 1080, "youtube.mp4"),
            "youtube_shorts_9_16" => new("youtube_shorts_9_16", "YouTube Shorts 9:16", 1080, 1920, "short.mp4"),
            _ => new("reels_9_16", "Reels 9:16", 1080, 1920, "reel.mp4")
        };
    }
}
