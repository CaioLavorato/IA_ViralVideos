namespace VideoSaaS.Infrastructure.Ai;

public sealed class OllamaOptions
{
    public string BaseUrl { get; set; } = "http://host.docker.internal:11434";
    public string Model { get; set; } = "qwen2.5:7b";
    public int TimeoutSeconds { get; set; } = 120;
}
