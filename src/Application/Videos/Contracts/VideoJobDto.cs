using VideoSaaS.Domain.Entities;
using VideoSaaS.Domain.Enums;
using VideoSaaS.Domain.ValueObjects;

namespace VideoSaaS.Application.Videos.Contracts;

public sealed record VideoJobDto(
    Guid Id,
    Guid TenantId,
    Guid UserId,
    string Theme,
    string Style,
    string Duration,
    string Tone,
    string Voice,
    int SceneCount,
    string ImageType,
    string Format,
    VideoJobStatus Status,
    List<SceneSpec> Scenes,
    string? ScriptJson,
    string? VideoPath,
    string? ReelPath,
    string? AudioPath,
    string? Error,
    DateTimeOffset CreatedAt)
{
    public static VideoJobDto FromEntity(VideoJob job) => new(
        job.Id, job.TenantId, job.UserId, job.Theme, job.Style, job.Duration,
        job.Tone, job.Voice, job.SceneCount, job.ImageType, job.Format,
        job.Status, job.Scenes, job.ScriptJson, job.VideoPath, job.ReelPath,
        job.AudioPath, job.Error, job.CreatedAt);
}
