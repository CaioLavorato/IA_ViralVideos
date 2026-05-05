using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VideoSaaS.Application.Abstractions;
using VideoSaaS.Domain.Entities;
using VideoSaaS.Domain.ValueObjects;

namespace VideoSaaS.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<VideoJob> VideoJobs => Set<VideoJob>();
    public DbSet<BillingRecord> Billing => Set<BillingRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.Slug).IsUnique();
            b.OwnsOne(x => x.Settings);
        });

        modelBuilder.Entity<AppUser>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
        });

        modelBuilder.Entity<BillingRecord>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Period }).IsUnique();
            b.Property(x => x.Period).HasConversion(v => v.ToString("yyyy-MM-dd"), v => DateOnly.Parse(v));
        });

        modelBuilder.Entity<VideoJob>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.CreatedAt });
            b.Property(x => x.Scenes)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<SceneSpec>>(v, (JsonSerializerOptions?)null) ?? new List<SceneSpec>())
                .Metadata.SetValueComparer(
                    new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<SceneSpec>>(
                        (c1, c2) => JsonSerializer.Serialize(c1, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(c2, (JsonSerializerOptions?)null),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => JsonSerializer.Deserialize<List<SceneSpec>>(JsonSerializer.Serialize(c, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null)!));
        });
    }
}
