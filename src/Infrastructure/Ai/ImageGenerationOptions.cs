namespace VideoSaaS.Infrastructure.Ai;

public sealed class ImageGenerationOptions
{
    public string Provider { get; set; } = "comfyui";
    public string? FalApiKey { get; set; }
    public string? ReplicateApiToken { get; set; }
    public string? TogetherApiKey { get; set; }
    public string? HuggingFaceToken { get; set; }
}
