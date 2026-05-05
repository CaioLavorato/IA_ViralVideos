using VideoSaaS.Domain.ValueObjects;

namespace VideoSaaS.Application.Abstractions;

public interface IImageGenerator
{
    Task<string> GenerateSceneImageAsync(Guid tenantId, Guid jobId, SceneSpec scene, string imageType, string format, CancellationToken cancellationToken);
}
