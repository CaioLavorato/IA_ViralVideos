using VideoSaaS.Domain.Abstractions;

namespace VideoSaaS.Domain.Entities;

public sealed class AppUser : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Role { get; set; } = "creator";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
