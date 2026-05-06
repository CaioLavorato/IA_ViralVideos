using VideoSaaS.Domain.Entities;
using VideoSaaS.Domain.ValueObjects;

namespace VideoSaaS.Application.Abstractions;

public interface IImageGenerator
{
    Task<string> GenerateSceneImageAsync(VideoJob job, SceneSpec scene, CancellationToken cancellationToken);
}
