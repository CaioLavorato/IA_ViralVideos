namespace VideoSaaS.Domain.Abstractions;

public interface ITenantEntity
{
    Guid TenantId { get; set; }
}
