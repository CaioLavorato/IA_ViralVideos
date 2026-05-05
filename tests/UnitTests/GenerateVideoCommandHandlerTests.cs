using Microsoft.EntityFrameworkCore;
using VideoSaaS.Application.Abstractions;
using VideoSaaS.Application.Videos.Contracts;
using VideoSaaS.Application.Videos.GenerateVideo;
using VideoSaaS.Domain.Entities;
using VideoSaaS.Infrastructure.Persistence;
using VideoSaaS.Infrastructure.Queue;
using Xunit;

namespace VideoSaaS.UnitTests;

public sealed class GenerateVideoCommandHandlerTests
{
    [Fact]
    public async Task GenerateVideo_creates_job_and_enqueues_it()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        db.Tenants.Add(new Tenant { Id = tenantId, Name = "Test", Slug = "test", Plan = "free" });
        db.Users.Add(new AppUser { Id = userId, TenantId = tenantId, Email = "test@example.local" });
        await db.SaveChangesAsync();

        var queue = new InMemoryJobQueue();
        var handler = new GenerateVideoCommandHandler(db, new TestTenantContext(tenantId, userId), queue);

        var result = await handler.Handle(new GenerateVideoCommand(new VideoGenerationRequest(
            "tema", "educativo", "curto", "viral", "pt_BR-cadu-medium", 3, "cinematic", "reels_9_16")), CancellationToken.None);

        Assert.Equal(tenantId, result.TenantId);
        Assert.Equal("tema", result.Theme);
        Assert.Equal(result.Id, await queue.DequeueAsync(CancellationToken.None));
    }

    private sealed record TestTenantContext(Guid TenantId, Guid UserId) : ITenantContext;
}
