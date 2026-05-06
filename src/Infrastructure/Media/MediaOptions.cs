namespace VideoSaaS.Infrastructure.Media;

public sealed class MediaOptions
{
    public string RootPath { get; set; } = "/app/media";
    public string? DockerBindRootPath { get; set; }
    public string PiperImage { get; set; } = "rhasspy/wyoming-piper:latest";
    public string FfmpegImage { get; set; } = "jrottenberg/ffmpeg:latest";
    public string PiperModelsPath { get; set; } = "/app/models/piper";
    public string DockerWorkdir { get; set; } = "/work";
}
