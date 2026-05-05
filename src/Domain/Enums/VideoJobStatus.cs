namespace VideoSaaS.Domain.Enums;

public enum VideoJobStatus
{
    Queued = 0,
    Processing = 1,
    GeneratingScript = 2,
    GeneratingImages = 3,
    GeneratingAudio = 4,
    RenderingVideo = 5,
    Completed = 6,
    Failed = 7
}
