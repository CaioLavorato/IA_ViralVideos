namespace VideoSaaS.Application.Abstractions;

public interface ITenantContext
{
    Guid TenantId { get; }
    Guid UserId { get; }
}
