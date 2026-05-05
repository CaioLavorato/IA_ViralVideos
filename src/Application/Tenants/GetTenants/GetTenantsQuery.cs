using MediatR;

namespace VideoSaaS.Application.Tenants.GetTenants;

public sealed record GetTenantsQuery : IRequest<IReadOnlyList<TenantDto>>;
