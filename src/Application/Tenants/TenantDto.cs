using VideoSaaS.Domain.Entities;

namespace VideoSaaS.Application.Tenants;

public sealed record TenantDto(Guid Id, string Name, string Slug, string Plan, TenantSettings Settings)
{
    public static TenantDto FromEntity(Tenant tenant) => new(tenant.Id, tenant.Name, tenant.Slug, tenant.Plan, tenant.Settings);
}
