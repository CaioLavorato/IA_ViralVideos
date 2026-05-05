using MediatR;
using Microsoft.EntityFrameworkCore;
using VideoSaaS.Application.Abstractions;
using VideoSaaS.Application.Tenants;

namespace VideoSaaS.Application.Tenants.GetTenants;

public sealed class GetTenantsQueryHandler(IAppDbContext db) : IRequestHandler<GetTenantsQuery, IReadOnlyList<TenantDto>>
{
    public async Task<IReadOnlyList<TenantDto>> Handle(GetTenantsQuery request, CancellationToken cancellationToken)
    {
        return await db.Tenants.AsNoTracking().OrderBy(t => t.Name).Select(t => TenantDto.FromEntity(t)).ToListAsync(cancellationToken);
    }
}
