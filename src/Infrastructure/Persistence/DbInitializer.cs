using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VideoSaaS.Domain.Entities;

namespace VideoSaaS.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
        var dbPath = db.Database.GetDbConnection().DataSource;
        var dbDirectory = Path.GetDirectoryName(Path.GetFullPath(dbPath));
        if (!string.IsNullOrWhiteSpace(dbDirectory))
        {
            Directory.CreateDirectory(dbDirectory);
        }

        await db.Database.EnsureCreatedAsync(cancellationToken);
        await EnsureColumnAsync(db, logger, "VideoJobs", "ImageProvider", "TEXT NOT NULL DEFAULT 'comfyui'", cancellationToken);
        await EnsureColumnAsync(db, logger, "VideoJobs", "ImageModel", "TEXT NOT NULL DEFAULT 'local'", cancellationToken);

        if (await db.Tenants.AnyAsync(cancellationToken))
        {
            return;
        }

        var tenant = new Tenant
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Demo Studio",
            Slug = "demo",
            Plan = "free",
            Settings = new TenantSettings
            {
                DefaultVoice = "pt_BR-cadu-medium",
                DefaultStyle = "educativo",
                MonthlyVideoLimit = 10,
                MaxScenesPerVideo = 8,
                MaxDurationSeconds = 90
            }
        };

        db.Tenants.Add(tenant);
        db.Users.Add(new AppUser
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            TenantId = tenant.Id,
            Email = "creator@demo.local",
            DisplayName = "Demo Creator"
        });
        db.Billing.Add(new BillingRecord { TenantId = tenant.Id, Plan = tenant.Plan });
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded demo tenant {TenantId}", tenant.Id);
    }

    private static async Task EnsureColumnAsync(
        AppDbContext db,
        ILogger logger,
        string tableName,
        string columnName,
        string definition,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{tableName}\");";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        await db.Database.ExecuteSqlRawAsync(
            $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{columnName}\" {definition};",
            cancellationToken);
        logger.LogInformation("Added missing SQLite column {Table}.{Column}", tableName, columnName);
    }
}
