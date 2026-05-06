using Microsoft.Extensions.Logging;
using VideoSaaS.Application.Abstractions;
using VideoSaaS.Domain.Entities;
using VideoSaaS.Domain.ValueObjects;

namespace VideoSaaS.Infrastructure.Ai;

public sealed class ImageGeneratorRouter(
    ComfyUiImageGenerator comfyUi,
    ExternalImageGenerator external,
    ILogger<ImageGeneratorRouter> logger) : IImageGenerator
{
    public Task<string> GenerateSceneImageAsync(VideoJob job, SceneSpec scene, CancellationToken cancellationToken)
    {
        var model = ImageGenerationModels.Resolve(job.ImageProvider, job.ImageModel);
        logger.LogInformation("Generating image for scene {SceneIndex} with {Provider}/{Model}", scene.Index, model.Provider, model.Model);

        return model.Provider.Equals("comfyui", StringComparison.OrdinalIgnoreCase)
            ? comfyUi.GenerateSceneImageAsync(job, scene, cancellationToken)
            : external.GenerateSceneImageAsync(job, scene, cancellationToken);
    }
}
