using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VideoSaaS.Application.Abstractions;
using VideoSaaS.Application.Videos.Contracts;
using VideoSaaS.Domain.Enums;
using VideoSaaS.Infrastructure.Persistence;

namespace VideoSaaS.Infrastructure.Media;

public sealed class VideoPipeline(
    AppDbContext db,
    IAiScriptGenerator scripts,
    IImageGenerator images,
    ITtsService tts,
    IVideoRenderer renderer,
    ILogger<VideoPipeline> logger) : IVideoPipeline
{
    public async Task ProcessAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await db.VideoJobs.FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken)
            ?? throw new InvalidOperationException($"Video job {jobId} not found.");

        try
        {
            job.Status = VideoJobStatus.GeneratingScript;
            job.StartedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            var request = new VideoGenerationRequest(job.Theme, job.Style, job.Duration, job.Tone, job.Voice, job.SceneCount, job.ImageType, job.Format, job.ImageProvider, job.ImageModel);
            var generated = await scripts.GenerateAsync(request, cancellationToken);
            job.Scenes = generated.Scenes;
            job.ScriptJson = generated.RawJson;
            await db.SaveChangesAsync(cancellationToken);

            job.Status = VideoJobStatus.GeneratingImages;
            await db.SaveChangesAsync(cancellationToken);
            foreach (var scene in job.Scenes)
            {
                scene.ImagePath = await images.GenerateSceneImageAsync(job, scene, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
            }

            job.Status = VideoJobStatus.GeneratingAudio;
            await db.SaveChangesAsync(cancellationToken);
            var audioFiles = new List<string>();
            foreach (var scene in job.Scenes)
            {
                scene.AudioPath = await tts.GenerateSceneAudioAsync(job.TenantId, job.Id, scene, job.Voice, cancellationToken);
                audioFiles.Add(scene.AudioPath);
                await db.SaveChangesAsync(cancellationToken);
            }

            logger.LogInformation("Audio files count: {Count}", audioFiles.Count);
            foreach (var file in audioFiles)
            {
                logger.LogInformation("Audio file: {File} Exists: {Exists}", file, File.Exists(file));
            }

            if (audioFiles.Count == 0)
            {
                throw new InvalidOperationException("Nenhum áudio foi gerado para este job.");
            }

            job.Status = VideoJobStatus.RenderingVideo;
            await db.SaveChangesAsync(cancellationToken);
            var result = await renderer.RenderAsync(job, audioFiles, cancellationToken);

            job.VideoPath = result.VideoPath;
            job.ReelPath = result.ReelPath;
            job.AudioPath = result.AudioPath;
            job.OutputDirectory = Path.GetDirectoryName(result.ReelPath);
            job.Status = VideoJobStatus.Completed;
            job.FinishedAt = DateTimeOffset.UtcNow;

            var period = new DateOnly(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, 1);
            var billing = await db.Billing.FirstOrDefaultAsync(b => b.TenantId == job.TenantId && b.Period == period, cancellationToken);
            if (billing is not null)
            {
                billing.TotalDurationSecondsThisMonth += (int)Math.Ceiling(job.Scenes.Sum(s => s.EstimatedSeconds));
                billing.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Video job {JobId} completed. Artifacts: {Artifacts}", job.Id, JsonSerializer.Serialize(new { job.ReelPath, job.AudioPath }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Video job {JobId} failed", job.Id);
            job.Status = VideoJobStatus.Failed;
            job.Error = ex.Message;
            job.FinishedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(CancellationToken.None);
        }
    }
}
