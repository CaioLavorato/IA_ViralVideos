using VideoSaaS.Domain.Abstractions;

namespace VideoSaaS.Domain.Entities;

public sealed class BillingRecord : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Plan { get; set; } = "free";
    public int VideosGeneratedThisMonth { get; set; }
    public int TotalDurationSecondsThisMonth { get; set; }
    public DateOnly Period { get; set; } = new(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, 1);
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
