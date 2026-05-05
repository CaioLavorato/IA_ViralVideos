using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VideoSaaS.Application.Abstractions;

namespace VideoSaaS.Workers;

public sealed class VideoGenerationWorker(
    IJobQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<VideoGenerationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Video generation worker started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            var jobId = await queue.DequeueAsync(stoppingToken);
            _ = Task.Run(() => ProcessJobAsync(jobId, stoppingToken), stoppingToken);
        }
    }

    private async Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var pipeline = scope.ServiceProvider.GetRequiredService<IVideoPipeline>();
        await pipeline.ProcessAsync(jobId, cancellationToken);
    }
}
