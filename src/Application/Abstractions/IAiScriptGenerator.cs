using VideoSaaS.Application.Videos.Contracts;

namespace VideoSaaS.Application.Abstractions;

public interface IAiScriptGenerator
{
    Task<GeneratedScriptDto> GenerateAsync(VideoGenerationRequest request, CancellationToken cancellationToken);
}
