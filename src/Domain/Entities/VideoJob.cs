using VideoSaaS.Domain.Abstractions;
using VideoSaaS.Domain.Enums;
using VideoSaaS.Domain.ValueObjects;

namespace VideoSaaS.Domain.Entities;

public sealed class VideoJob : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string Theme { get; set; } = "";
    public string Style { get; set; } = "";
    public string Duration { get; set; } = "curto";
    public string Tone { get; set; } = "";
    public string Voice { get; set; } = "pt_BR-cadu-medium";
    public int SceneCount { get; set; } = 3;
    public string ImageType { get; set; } = "cinematic";
    public string ImageProvider { get; set; } = "comfyui";
    public string ImageModel { get; set; } = "local";
    public string Format { get; set; } = "reels_9_16";
    public VideoJobStatus Status { get; set; } = VideoJobStatus.Queued;
    public List<SceneSpec> Scenes { get; set; } = [];
    public string? ScriptJson { get; set; }
    public string? OutputDirectory { get; set; }
    public string? VideoPath { get; set; }
    public string? ReelPath { get; set; }
    public string? AudioPath { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
}
