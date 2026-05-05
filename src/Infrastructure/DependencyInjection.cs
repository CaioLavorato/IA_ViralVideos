using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VideoSaaS.Application.Abstractions;
using VideoSaaS.Infrastructure.Ai;
using VideoSaaS.Infrastructure.Media;
using VideoSaaS.Infrastructure.Persistence;
using VideoSaaS.Infrastructure.Queue;
using VideoSaaS.Infrastructure.Tenancy;

namespace VideoSaaS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OllamaOptions>(configuration.GetSection("Ollama"));
        services.Configure<ComfyUiOptions>(configuration.GetSection("ComfyUI"));
        services.Configure<MediaOptions>(configuration.GetSection("Media"));

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("Default") ?? "Data Source=data/videosaas.db"));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<ITenantContext, HeaderTenantContext>();
        services.AddSingleton<IJobQueue, InMemoryJobQueue>();
        services.AddSingleton<MediaPathBuilder>();
        services.AddHttpClient<IAiScriptGenerator, OllamaScriptGenerator>((sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OllamaOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
        services.AddHttpClient<IImageGenerator, ComfyUiImageGenerator>((sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ComfyUiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
        services.AddScoped<ITtsService, PiperTtsService>();
        services.AddScoped<IVideoRenderer, DockerFfmpegVideoRenderer>();
        services.AddScoped<IVideoPipeline, VideoPipeline>();
        return services;
    }
}
