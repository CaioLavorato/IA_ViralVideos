using VideoSaaS.Domain.Entities;

namespace VideoSaaS.Application.Abstractions;

public interface IVideoRenderer
{
    Task<RenderedVideoResult> RenderAsync(VideoJob job, IReadOnlyList<string> audioFiles, CancellationToken cancellationToken);
}
