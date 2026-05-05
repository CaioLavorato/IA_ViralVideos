using MediatR;
using Microsoft.EntityFrameworkCore;
using VideoSaaS.Application.Abstractions;
using VideoSaaS.Application.Videos.Contracts;
using VideoSaaS.Domain.Entities;
using VideoSaaS.Domain.Enums;

namespace VideoSaaS.Application.Videos.GenerateVideo;

public sealed class GenerateVideoCommandHandler(
    IAppDbContext db,
    ITenantContext tenantContext,
    IJobQueue queue) : IRequestHandler<GenerateVideoCommand, VideoJobDto>
{
    public async Task<VideoJobDto> Handle(GenerateVideoCommand command, CancellationToken cancellationToken)
    {
        var tenant = await db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantContext.TenantId, cancellationToken)
            ?? throw new InvalidOperationException("Tenant not found.");

        var request = command.Request with
        {
            Voice = string.IsNullOrWhiteSpace(command.Request.Voice) ? tenant.Settings.DefaultVoice : command.Request.Voice,
            Style = string.IsNullOrWhiteSpace(command.Request.Style) ? tenant.Settings.DefaultStyle : command.Request.Style,
            SceneCount = Math.Clamp(command.Request.SceneCount, 1, tenant.Settings.MaxScenesPerVideo)
        };

        var period = new DateOnly(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, 1);
        var usage = await db.Billing.FirstOrDefaultAsync(b => b.TenantId == tenant.Id && b.Period == period, cancellationToken);
        usage ??= new BillingRecord { TenantId = tenant.Id, Plan = tenant.Plan, Period = period };
        if (usage.Id == Guid.Empty || db.Billing.Local.All(b => b.Id != usage.Id))
        {
            db.Billing.Add(usage);
        }

        // if (tenant.Plan.Equals("free", StringComparison.OrdinalIgnoreCase) &&
        //     usage.VideosGeneratedThisMonth >= tenant.Settings.MonthlyVideoLimit)
        // {
        //     throw new InvalidOperationException("Free plan generation limit reached for this tenant.");
        // }

        var job = new VideoJob
        {
            TenantId = tenantContext.TenantId,
            UserId = tenantContext.UserId,
            Theme = request.Theme,
            Style = request.Style,
            Duration = request.Duration,
            Tone = request.Tone,
            Voice = request.Voice,
            SceneCount = request.SceneCount,
            ImageType = request.ImageType,
            Format = request.Format,
            Status = VideoJobStatus.Queued
        };

        db.VideoJobs.Add(job);
        usage.VideosGeneratedThisMonth += 1;
        usage.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await queue.EnqueueAsync(job.Id, cancellationToken);
        return VideoJobDto.FromEntity(job);
    }
}
