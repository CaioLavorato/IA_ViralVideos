using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VideoSaaS.Application.Abstractions;
using VideoSaaS.Domain.ValueObjects;
using VideoSaaS.Infrastructure.Media;

namespace VideoSaaS.Infrastructure.Ai;

public sealed class ComfyUiImageGenerator(
    HttpClient httpClient,
    IOptions<ComfyUiOptions> options,
    MediaPathBuilder paths,
    ILogger<ComfyUiImageGenerator> logger) : IImageGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<string> GenerateSceneImageAsync(Guid tenantId, Guid jobId, SceneSpec scene, string imageType, string format, CancellationToken cancellationToken)
    {
        var checkpoint = await GetCheckpointNameAsync(cancellationToken);
        var preset = VideoFormatPreset.FromCode(format);
        var prompt = $"{scene.ImagePrompt}, {imageType}, {preset.AspectRatioPrompt}, high detail, cinematic lighting";
        var payload = BuildPromptPayload(prompt, jobId, scene.Index, checkpoint, options.Value, preset);
        logger.LogInformation("Sending scene {SceneIndex} to ComfyUI at {BaseUrl}", scene.Index, options.Value.BaseUrl);

        var response = await httpClient.PostAsJsonAsync("/prompt", payload, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var promptDoc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var promptId = promptDoc.RootElement.GetProperty("prompt_id").GetString()
            ?? throw new InvalidOperationException("ComfyUI did not return prompt_id.");

        var fileName = await WaitForImageAsync(promptId, cancellationToken);
        var bytes = await httpClient.GetByteArrayAsync($"/view?filename={Uri.EscapeDataString(fileName)}&type=output", cancellationToken);

        var jobDir = paths.GetJobDirectory(tenantId, jobId);
        var imagePath = Path.Combine(jobDir, $"scene-{scene.Index:00}.png");
        await File.WriteAllBytesAsync(imagePath, bytes, cancellationToken);
        return imagePath;
    }

    private async Task<string> GetCheckpointNameAsync(CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync("/object_info/CheckpointLoaderSimple", cancellationToken);
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

        var ckptInfo = doc.RootElement
            .GetProperty("CheckpointLoaderSimple")
            .GetProperty("input")
            .GetProperty("required")
            .GetProperty("ckpt_name");

        var values = ckptInfo[0];
        if (values.ValueKind == JsonValueKind.Object && values.TryGetProperty("value", out var objectValues))
        {
            values = objectValues;
        }

        var checkpoint = values.ValueKind == JsonValueKind.Array
            ? values.EnumerateArray().Select(v => v.GetString()).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))
            : values.GetString();

        if (string.IsNullOrWhiteSpace(checkpoint))
        {
            throw new InvalidOperationException("ComfyUI is running, but no image checkpoint was found. Add a .safetensors or .ckpt model to ComfyUI/models/checkpoints and restart ComfyUI.");
        }

        return checkpoint;
    }

    private static object BuildPromptPayload(string prompt, Guid jobId, int sceneIndex, string checkpoint, ComfyUiOptions options, VideoFormatPreset preset)
    {
        var seed = Math.Abs(HashCode.Combine(jobId, sceneIndex));
        var steps = Math.Clamp(options.Steps, 1, 50);
        var scale = Math.Clamp(options.DimensionScale, 0.2, 1.0);
        var width = options.Width > 0 ? options.Width : RoundToMultipleOfEight(preset.Width * scale);
        var height = options.Height > 0 ? options.Height : RoundToMultipleOfEight(preset.Height * scale);
        width = Math.Clamp(width, 256, 2048);
        height = Math.Clamp(height, 256, 2048);
        return new
        {
            prompt = new Dictionary<string, object>
            {
                ["3"] = new { class_type = "KSampler", inputs = new { cfg = 7, denoise = 1, latent_image = Ref("5", 0), model = Ref("4", 0), negative = Ref("7", 0), positive = Ref("6", 0), sampler_name = "euler", scheduler = "normal", seed, steps } },
                ["4"] = new { class_type = "CheckpointLoaderSimple", inputs = new { ckpt_name = checkpoint } },
                ["5"] = new { class_type = "EmptyLatentImage", inputs = new { batch_size = 1, height, width } },
                ["6"] = new { class_type = "CLIPTextEncode", inputs = new { clip = Ref("4", 1), text = prompt } },
                ["7"] = new { class_type = "CLIPTextEncode", inputs = new { clip = Ref("4", 1), text = "low quality, blurry, watermark, text, logo, distorted" } },
                ["8"] = new { class_type = "VAEDecode", inputs = new { samples = Ref("3", 0), vae = Ref("4", 2) } },
                ["9"] = new { class_type = "SaveImage", inputs = new { filename_prefix = $"videosaas_{jobId:N}_{sceneIndex:00}", images = Ref("8", 0) } }
            }
        };
    }

    private static object[] Ref(string node, int output) => [node, output];

    private static int RoundToMultipleOfEight(double value) => Math.Max(256, (int)Math.Round(value / 8) * 8);

    private async Task<string> WaitForImageAsync(string promptId, CancellationToken cancellationToken)
    {
        var attempts = Math.Max(30, options.Value.TimeoutSeconds / 2);
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            var history = await httpClient.GetFromJsonAsync<JsonElement>($"/history/{promptId}", JsonOptions, cancellationToken);
            if (history.TryGetProperty(promptId, out var item) &&
                item.TryGetProperty("outputs", out var outputs))
            {
                foreach (var output in outputs.EnumerateObject())
                {
                    if (output.Value.TryGetProperty("images", out var images) && images.GetArrayLength() > 0)
                    {
                        return images[0].GetProperty("filename").GetString()
                            ?? throw new InvalidOperationException("ComfyUI image filename is empty.");
                    }
                }
            }
        }

        throw new TimeoutException($"Timed out waiting for ComfyUI image generation after {options.Value.TimeoutSeconds} seconds.");
    }
}
