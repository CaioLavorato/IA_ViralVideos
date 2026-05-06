using VideoSaaS.Domain.ValueObjects;

namespace VideoSaaS.Infrastructure.Ai;

public static class ImageGenerationModels
{
    public static readonly IReadOnlyDictionary<string, ImageModelInfo> Models = new Dictionary<string, ImageModelInfo>(StringComparer.OrdinalIgnoreCase)
    {
        ["local"] = new("comfyui", "local", "ComfyUI local", "Modelo local atual do ComfyUI"),
        ["pollinations-flux"] = new("pollinations", "flux", "Pollinations FLUX (Grátis)", "100% gratuito e rápido"),
        ["fal-flux-schnell"] = new("fal", "fal-ai/flux/schnell", "fal.ai FLUX (Turbo)", "O mais rápido e estável - Qualidade incrível em 2s"),
        ["hf-flux-schnell"] = new("huggingface", "black-forest-labs/FLUX.1-schnell", "Hugging Face FLUX schnell", "Rápido e geralmente disponível no Inference API"),
        ["hf-realvis-xl"] = new("huggingface", "SG161222/RealVisXL_V4.0", "Hugging Face RealVisXL V4.0", "Ultra-realismo, ótimo para vídeos dark/virais"),
        ["hf-juggernaut-xl"] = new("huggingface", "RunDiffusion/Juggernaut-XL-v9", "Hugging Face Juggernaut XL", "Estética viral, muito estável na API"),
        ["hf-flux-dev"] = new("huggingface", "black-forest-labs/FLUX.1-schnell", "Hugging Face FLUX.1 dev", "Fallback para Schnell devido a limitações da API gratuita")
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
            "cartoon" => "clean modern editorial illustration, expressive but natural anatomy, 4k",
            "realista" => "hyper-realistic documentary photo, high detail, 8k, natural lighting, sharp focus",
            _ => "cinematic film still, high detail, 8k, moody lighting, professional composition"
        };

        return string.Join(", ", [
            scene.ImagePrompt,
            style,
            preset.AspectRatioPrompt,
            "no emojis",
            "no floating icons",
            "no interface elements",
            "clean image",
            "high resolution",
            "professional color grading"
        ]);
    }

    public const string NegativePrompt = "emojis, icons, computer interface, buttons, text, watermark, logo, blurry, distorted face, bad hands, extra fingers, malformed, low quality, cartoonish (when realistic selected), messy background";
}

public sealed record ImageModelInfo(string Provider, string Model, string Label, string Description);
