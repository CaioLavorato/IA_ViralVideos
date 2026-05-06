using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VideoSaaS.Application.Abstractions;
using VideoSaaS.Domain.Entities;
using VideoSaaS.Domain.ValueObjects;
using VideoSaaS.Infrastructure.Media;

namespace VideoSaaS.Infrastructure.Ai;

public sealed class ExternalImageGenerator(
    HttpClient httpClient,
    IOptions<ImageGenerationOptions> options,
    MediaPathBuilder paths,
    ILogger<ExternalImageGenerator> logger) : IImageGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<string> GenerateSceneImageAsync(VideoJob job, SceneSpec scene, CancellationToken cancellationToken)
    {
        var model = ImageGenerationModels.Resolve(job.ImageProvider, job.ImageModel);
        var key = GetProviderKey(model.Provider);
        if (string.IsNullOrWhiteSpace(key) && !model.Provider.Equals("pollinations", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Provedor de imagem '{model.Provider}' selecionado, mas a API key não foi configurada. Configure ImageGeneration__{ProviderKeyName(model.Provider)} no ambiente.");
        }

        var preset = VideoFormatPreset.FromCode(job.Format);
        var prompt = ImageGenerationModels.BuildPrompt(scene, job.ImageType, preset);
        
        byte[] bytes;
        if (model.Provider.Equals("huggingface", StringComparison.OrdinalIgnoreCase))
        {
            bytes = await GenerateWithHuggingFaceAsync(model, key, prompt, preset, cancellationToken);
        }
        else if (model.Provider.Equals("fal", StringComparison.OrdinalIgnoreCase))
        {
            bytes = await GenerateWithFalAsync(model, key, prompt, preset, cancellationToken);
        }
        else if (model.Provider.Equals("pollinations", StringComparison.OrdinalIgnoreCase))
        {
            bytes = await GenerateWithPollinationsAsync(model, prompt, preset, cancellationToken);
        }
        else
        {
            throw new NotSupportedException($"Provider '{model.Provider}' ainda não está ativo no backend.");
        }

        var jobDir = paths.GetJobDirectory(job.TenantId, job.Id);
        Directory.CreateDirectory(jobDir);
        var imagePath = Path.Combine(jobDir, $"scene-{scene.Index:00}.png");
        await File.WriteAllBytesAsync(imagePath, bytes, cancellationToken);
        logger.LogInformation("Saved external image for scene {SceneIndex} at {ImagePath}", scene.Index, imagePath);
        return imagePath;
    }

    private async Task<byte[]> GenerateWithPollinationsAsync(
        ImageModelInfo model,
        string prompt,
        VideoFormatPreset preset,
        CancellationToken cancellationToken)
    {
        // Pollinations.ai usa uma URL simples: https://image.pollinations.ai/prompt/{prompt}?width={w}&height={h}&nologo=true&model=flux
        var encodedPrompt = Uri.EscapeDataString(prompt);
        var url = $"https://image.pollinations.ai/prompt/{encodedPrompt}?width={preset.Width}&height={preset.Height}&nologo=true&model=flux&seed={Random.Shared.Next()}";
        
        logger.LogInformation("Calling Pollinations.ai (Free) with prompt: {Prompt}", prompt);
        return await httpClient.GetByteArrayAsync(url, cancellationToken);
    }

    private async Task<byte[]> GenerateWithHuggingFaceAsync(
        ImageModelInfo model,
        string token,
        string prompt,
        VideoFormatPreset preset,
        CancellationToken cancellationToken)
    {
        var endpoint = $"https://api-inference.huggingface.co/models/{model.Model}";
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new
        {
            inputs = prompt,
            parameters = new
            {
                negative_prompt = ImageGenerationModels.NegativePrompt,
                width = ClampDimension(preset.Width),
                height = ClampDimension(preset.Height),
                num_inference_steps = 28,
                guidance_scale = 3.5
            }
        }, options: JsonOptions);

        logger.LogInformation("Calling Hugging Face image provider with model {Model}", model.Model);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
        var responseBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var message = TryReadUtf8(responseBytes);
            throw new InvalidOperationException($"Hugging Face image generation failed ({(int)response.StatusCode}): {message}");
        }

        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return responseBytes;
        }

        if (contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            return await ExtractImageFromJsonAsync(responseBytes, token, cancellationToken);
        }

        return responseBytes;
    }

    private async Task<byte[]> ExtractImageFromJsonAsync(byte[] responseBytes, string token, CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(responseBytes);
        var root = doc.RootElement;

        if (TryGetString(root, out var directValue))
        {
            return await ResolveImageValueAsync(directValue, token, cancellationToken);
        }

        if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
        {
            return await ExtractImageFromJsonAsync(JsonSerializer.SerializeToUtf8Bytes(root[0]), token, cancellationToken);
        }

        foreach (var property in new[] { "image", "url", "data", "generated_image", "output" })
        {
            if (root.TryGetProperty(property, out var value))
            {
                if (TryGetString(value, out var imageValue))
                {
                    return await ResolveImageValueAsync(imageValue, token, cancellationToken);
                }

                if (value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                {
                    return await ExtractImageFromJsonAsync(JsonSerializer.SerializeToUtf8Bytes(value), token, cancellationToken);
                }
            }
        }

        throw new InvalidOperationException($"Hugging Face returned JSON without an image payload: {TryReadUtf8(responseBytes)}");
    }

    private async Task<byte[]> ResolveImageValueAsync(string value, string token, CancellationToken cancellationToken)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }

        var commaIndex = value.IndexOf(',');
        var base64 = value.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && commaIndex >= 0
            ? value[(commaIndex + 1)..]
            : value;
        return Convert.FromBase64String(base64);
    }

    private async Task<byte[]> GenerateWithFalAsync(
        ImageModelInfo model,
        string apiKey,
        string prompt,
        VideoFormatPreset preset,
        CancellationToken cancellationToken)
    {
        var endpoint = $"https://queue.fal.run/{model.Model}";
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Key", apiKey);
        
        var payload = new
        {
            prompt,
            image_size = preset.Code == "youtube_16_9" ? "landscape_16_9" : "portrait_9_16",
            num_inference_steps = 4,
            enable_safety_checker = false
        };

        request.Content = JsonContent.Create(payload, options: JsonOptions);

        logger.LogInformation("Calling Fal.ai provider with model {Model}", model.Model);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Fal.ai generation failed: {error}");
        }

        var queueResult = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        var requestId = queueResult.GetProperty("request_id").GetString();
        
        // Polling simples para o resultado
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000, cancellationToken);
            using var pollReq = new HttpRequestMessage(HttpMethod.Get, $"https://queue.fal.run/requests/{requestId}/status");
            pollReq.Headers.Authorization = new AuthenticationHeaderValue("Key", apiKey);
            using var pollRes = await httpClient.SendAsync(pollReq, cancellationToken);
            
            var statusDoc = await pollRes.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            var status = statusDoc.GetProperty("status").GetString();
            
            if (status == "COMPLETED")
            {
                using var resultReq = new HttpRequestMessage(HttpMethod.Get, $"https://queue.fal.run/requests/{requestId}");
                resultReq.Headers.Authorization = new AuthenticationHeaderValue("Key", apiKey);
                using var resultRes = await httpClient.SendAsync(resultReq, cancellationToken);
                var result = await resultRes.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
                
                var imageUrl = result.GetProperty("images")[0].GetProperty("url").GetString()!;
                return await httpClient.GetByteArrayAsync(imageUrl, cancellationToken);
            }
            
            if (status == "FAILED")
            {
                throw new InvalidOperationException("Fal.ai task failed");
            }
        }

        throw new OperationCanceledException();
    }

    private string? GetProviderKey(string provider) => provider.ToLowerInvariant() switch
    {
        "fal" => options.Value.FalApiKey,
        "replicate" => options.Value.ReplicateApiToken,
        "together" => options.Value.TogetherApiKey,
        "huggingface" => options.Value.HuggingFaceToken,
        _ => null
    };

    private static bool TryGetString(JsonElement element, out string value)
    {
        value = "";
        if (element.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = element.GetString() ?? "";
        return !string.IsNullOrWhiteSpace(value);
    }

    private static int ClampDimension(int value) => Math.Clamp((int)Math.Round(value / 16.0) * 16, 512, 1536);

    private static string TryReadUtf8(byte[] bytes)
    {
        try
        {
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return "<binary response>";
        }
    }

    private static string ProviderKeyName(string provider) => provider.ToLowerInvariant() switch
    {
        "fal" => "FalApiKey",
        "replicate" => "ReplicateApiToken",
        "together" => "TogetherApiKey",
        "huggingface" => "HuggingFaceToken",
        _ => "ApiKey"
    };
}
