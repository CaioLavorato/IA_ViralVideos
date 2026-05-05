using Microsoft.EntityFrameworkCore;
using VideoSaaS.Domain.Entities;

namespace VideoSaaS.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<AppUser> Users { get; }
    DbSet<VideoJob> VideoJobs { get; }
    DbSet<BillingRecord> Billing { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
