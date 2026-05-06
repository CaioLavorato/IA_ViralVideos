using Microsoft.Extensions.Logging;
using VideoSaaS.Application.Abstractions;
using VideoSaaS.Domain.Entities;

namespace VideoSaaS.Infrastructure.Media;

public sealed class VideoArtifactStore(
    MediaPathBuilder paths,
    ILogger<VideoArtifactStore> logger) : IVideoArtifactStore
{
    public Task DeleteArtifactsAsync(VideoJob job, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var jobDir = paths.GetJobDirectory(job.TenantId, job.Id);
        var root = paths.GetRootDirectory();
        var fullJobDir = Path.GetFullPath(jobDir);
        var fullRoot = Path.GetFullPath(root);

        if (!fullJobDir.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Artifact directory outside media root: {fullJobDir}");
        }

        if (Directory.Exists(fullJobDir))
        {
            Directory.Delete(fullJobDir, recursive: true);
            logger.LogInformation("Deleted artifacts for video job {JobId}: {JobDir}", job.Id, fullJobDir);
        }

        return Task.CompletedTask;
    }
}
