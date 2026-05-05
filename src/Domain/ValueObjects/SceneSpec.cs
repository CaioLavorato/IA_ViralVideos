namespace VideoSaaS.Domain.ValueObjects;

public sealed class SceneSpec
{
    public int Index { get; set; }
    public string Text { get; set; } = "";
    public string ImagePrompt { get; set; } = "";
    public string? ImagePath { get; set; }
    public string? AudioPath { get; set; }
    public double EstimatedSeconds { get; set; } = 5;
}
