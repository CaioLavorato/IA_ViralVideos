namespace VideoSaaS.Application.Abstractions;

public interface IVideoPipeline
{
    Task ProcessAsync(Guid jobId, CancellationToken cancellationToken);
}
