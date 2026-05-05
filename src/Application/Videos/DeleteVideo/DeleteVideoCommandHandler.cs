using MediatR;
using Microsoft.EntityFrameworkCore;
using VideoSaaS.Application.Abstractions;

namespace VideoSaaS.Application.Videos.DeleteVideo;

public sealed class DeleteVideoCommandHandler(
    IAppDbContext db,
    ITenantContext tenantContext) : IRequestHandler<DeleteVideoCommand>
{
    public async Task Handle(DeleteVideoCommand request, CancellationToken cancellationToken)
    {
        var job = await db.VideoJobs
            .FirstOrDefaultAsync(j => j.Id == request.Id && j.TenantId == tenantContext.TenantId, cancellationToken);

        if (job is not null)
        {
            db.VideoJobs.Remove(job);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
