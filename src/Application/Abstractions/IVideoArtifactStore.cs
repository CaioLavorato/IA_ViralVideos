using VideoSaaS.Domain.Entities;

namespace VideoSaaS.Application.Abstractions;

public interface IVideoArtifactStore
{
    Task DeleteArtifactsAsync(VideoJob job, CancellationToken cancellationToken);
}
