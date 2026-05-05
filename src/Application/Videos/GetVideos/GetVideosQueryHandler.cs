using MediatR;
using Microsoft.EntityFrameworkCore;
using VideoSaaS.Application.Abstractions;
using VideoSaaS.Application.Videos.Contracts;

namespace VideoSaaS.Application.Videos.GetVideos;

public sealed class GetVideosQueryHandler(IAppDbContext db, ITenantContext tenantContext) : IRequestHandler<GetVideosQuery, IReadOnlyList<VideoJobDto>>
{
    public async Task<IReadOnlyList<VideoJobDto>> Handle(GetVideosQuery request, CancellationToken cancellationToken)
    {
        var jobs = await db.VideoJobs.AsNoTracking()
            .Where(v => v.TenantId == tenantContext.TenantId)
            .ToListAsync(cancellationToken);

        return jobs
            .OrderByDescending(v => v.CreatedAt)
            .Select(VideoJobDto.FromEntity)
            .ToList();
    }
}
