namespace VideoSaaS.Infrastructure.Ai;

public sealed class ComfyUiOptions
{
    public string BaseUrl { get; set; } = "http://host.docker.internal:8188";
    public int TimeoutSeconds { get; set; } = 1800;
    public int Steps { get; set; } = 10;
    public double DimensionScale { get; set; } = 0.5;
    public int Width { get; set; }
    public int Height { get; set; }
    public string WorkflowFile { get; set; } = "/app/docker/media-pipeline/comfyui-workflow.json";
}
