using VideoSaaS.Domain.ValueObjects;

namespace VideoSaaS.Application.Abstractions;

public interface ITtsService
{
    Task<string> GenerateSceneAudioAsync(Guid tenantId, Guid jobId, SceneSpec scene, string voice, CancellationToken cancellationToken);
}
