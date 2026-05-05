using Microsoft.AspNetCore.Http;
using VideoSaaS.Application.Abstractions;

namespace VideoSaaS.Infrastructure.Tenancy;

public sealed class HeaderTenantContext(IHttpContextAccessor accessor) : ITenantContext
{
    private static readonly Guid DemoTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DemoUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public Guid TenantId => ReadGuid("X-Tenant-Id", DemoTenantId);
    public Guid UserId => ReadGuid("X-User-Id", DemoUserId);

    private Guid ReadGuid(string header, Guid fallback)
    {
        var value = accessor.HttpContext?.Request.Headers[header].FirstOrDefault();
        return Guid.TryParse(value, out var parsed) ? parsed : fallback;
    }
}
