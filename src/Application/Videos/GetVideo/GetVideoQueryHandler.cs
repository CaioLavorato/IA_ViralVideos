using MediatR;
using Microsoft.EntityFrameworkCore;
using VideoSaaS.Application.Abstractions;
using VideoSaaS.Application.Videos.Contracts;

namespace VideoSaaS.Application.Videos.GetVideo;

public sealed class GetVideoQueryHandler(IAppDbContext db, ITenantContext tenantContext) : IRequestHandler<GetVideoQuery, VideoJobDto?>
{
    public async Task<VideoJobDto?> Handle(GetVideoQuery request, CancellationToken cancellationToken)
    {
        var job = await db.VideoJobs.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == request.Id && v.TenantId == tenantContext.TenantId, cancellationToken);
        return job is null ? null : VideoJobDto.FromEntity(job);
    }
}
