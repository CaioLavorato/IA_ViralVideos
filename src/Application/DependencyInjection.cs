using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VideoSaaS.Application.Videos.GenerateVideo;

namespace VideoSaaS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GenerateVideoCommand>());
        return services;
    }
}
