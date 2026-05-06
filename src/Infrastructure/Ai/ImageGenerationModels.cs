using VideoSaaS.Domain.ValueObjects;

namespace VideoSaaS.Infrastructure.Ai;

public static class ImageGenerationModels
{
    public static readonly IReadOnlyDictionary<string, ImageModelInfo> Models = new Dictionary<string, ImageModelInfo>(StringComparer.OrdinalIgnoreCase)
    {
        ["local"] = new("comfyui", "local", "ComfyUI local", "Modelo local atual do ComfyUI"),
        ["fal-flux-schnell"] = new("fal", "fal-ai/flux/schnell", "fal.ai FLUX.1 schnell", "Rápido, licença mais amigável, ótimo para protótipo"),
        ["fal-flux-dev"] = new("fal", "fal-ai/flux/dev", "fal.ai FLUX.1 dev", "Qualidade maior, confira licença antes de uso comercial"),
        ["replicate-flux-schnell"] = new("replicate", "black-forest-labs/flux-schnell", "Replicate FLUX.1 schnell", "Boa opção por API com setup simples"),
        ["replicate-flux-dev"] = new("replicate", "black-forest-labs/flux-dev", "Replicate FLUX.1 dev", "Qualidade maior, confira licença antes de uso comercial"),
        ["together-flux-schnell"] = new("together", "black-forest-labs/FLUX.1-schnell", "Together FLUX.1 schnell", "API rápida para geração de imagem"),
        ["hf-flux-dev"] = new("huggingface", "black-forest-labs/FLUX.1-dev", "Hugging Face FLUX.1 dev", "Usa Inference Providers; exige token e aceite da licença")
    };

    public static ImageModelInfo Resolve(string? provider, string? model)
    {
        if (!string.IsNullOrWhiteSpace(model) && Models.TryGetValue(model, out var byKey))
        {
            return byKey;
        }

        if (!string.IsNullOrWhiteSpace(model))
        {
            var normalizedProvider = string.IsNullOrWhiteSpace(provider) ? "comfyui" : provider.Trim().ToLowerInvariant();
            return new ImageModelInfo(normalizedProvider, model, model, "Modelo customizado");
        }

        return Models["local"];
    }

    public static string BuildPrompt(SceneSpec scene, string imageType, VideoFormatPreset preset)
    {
        var style = imageType.Trim().ToLowerInvariant() switch
        {
            "cartoon" => "clean modern editorial illustration, expressive but natural anatomy",
            "realista" => "realistic documentary photo, natural skin texture, believable anatomy",
            _ => "realistic cinematic social media still, natural skin texture, believable anatomy"
        };

        return string.Join(", ", [
            scene.ImagePrompt,
            style,
            preset.AspectRatioPrompt,
            "adult subject when a person is shown",
            "modern Brazilian social media context",
            "clear composition with one main subject",
            "natural facial expression",
            "proportional hands and fingers",
            "smartphone must look like a normal modern phone when present",
            "no readable text inside the image",
            "soft daylight or clean studio lighting",
            "sharp focus",
            "high detail"
        ]);
    }

    public const string NegativePrompt = "low quality, blurry, watermark, text, logo, caption, poster, meme text, distorted face, deformed face, uncanny, plastic skin, doll face, oversized smile, bad teeth, distorted eyes, crossed eyes, bad hands, extra fingers, missing fingers, fused fingers, warped phone, malformed smartphone, child, teenager, nude, nsfw";
}

public sealed record ImageModelInfo(string Provider, string Model, string Label, string Description);
