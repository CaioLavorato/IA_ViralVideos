using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VideoSaaS.Application.Abstractions;
using VideoSaaS.Application.Videos.Contracts;
using VideoSaaS.Domain.ValueObjects;

namespace VideoSaaS.Infrastructure.Ai;

public sealed class OllamaScriptGenerator(
    HttpClient httpClient,
    IOptions<OllamaOptions> options,
    ILogger<OllamaScriptGenerator> logger) : IAiScriptGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<GeneratedScriptDto> GenerateAsync(VideoGenerationRequest request, CancellationToken cancellationToken)
    {
        var preset = VideoFormatPreset.FromCode(request.Format);

        var prompt = $$"""
        Gere um roteiro em portugues do Brasil para um video no formato {{preset.Label}}.
        Tema: {{request.Theme}}
        Estilo: {{request.Style}}
        Duração: {{request.Duration}}
        Tom: {{request.Tone}}
        Número de cenas: {{request.SceneCount}}
        Tipo de imagem: {{request.ImageType}}

        Responda somente JSON válido, sem markdown, neste formato:
        {
          "cenas": [
            { "texto": "narração curta da cena", "prompt_imagem": "prompt visual detalhado em português" }
          ]
        }
        """;

        var payload = new
        {
            model = options.Value.Model,
            prompt,
            stream = false,
            format = "json",
            options = new { temperature = 0.7 }
        };

        logger.LogInformation("Calling Ollama at {BaseUrl} with model {Model}", options.Value.BaseUrl, options.Value.Model);
        var response = await httpClient.PostAsJsonAsync("/api/generate", payload, JsonOptions, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            var fallbackModel = await TryGetFirstAvailableModelAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(fallbackModel) && !fallbackModel.Equals(options.Value.Model, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Configured Ollama model {ConfiguredModel} was not found. Retrying with {FallbackModel}.", options.Value.Model, fallbackModel);
                payload = new
                {
                    model = fallbackModel,
                    prompt,
                    stream = false,
                    format = "json",
                    options = new { temperature = 0.7 }
                };
                response = await httpClient.PostAsJsonAsync("/api/generate", payload, JsonOptions, cancellationToken);
            }
        }

        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var raw = doc.RootElement.GetProperty("response").GetString() ?? "{}";
        return ParseScenes(raw, request.SceneCount);
    }

    private async Task<string?> TryGetFirstAvailableModelAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var tags = await httpClient.GetAsync("/api/tags", cancellationToken);
            if (!tags.IsSuccessStatusCode)
            {
                return null;
            }

            using var doc = JsonDocument.Parse(await tags.Content.ReadAsStringAsync(cancellationToken));
            var models = doc.RootElement.GetProperty("models");
            if (models.GetArrayLength() == 0)
            {
                return null;
            }

            return models[0].GetProperty("name").GetString();
        }
        catch
        {
            return null;
        }
    }

    private static GeneratedScriptDto ParseScenes(string raw, int expectedScenes)
    {
        using var script = JsonDocument.Parse(raw);
        var scenesElement = script.RootElement.TryGetProperty("cenas", out var cenas)
            ? cenas
            : script.RootElement.GetProperty("scenes");

        var scenes = new List<SceneSpec>();
        var index = 1;
        foreach (var item in scenesElement.EnumerateArray().Take(expectedScenes))
        {
            var text = ReadString(item, "texto", "text");
            var prompt = ReadString(item, "prompt_imagem", "image_prompt", "prompt");
            scenes.Add(new SceneSpec
            {
                Index = index++,
                Text = text,
                ImagePrompt = prompt,
                EstimatedSeconds = Math.Clamp(text.Length / 14.0, 4, 12)
            });
        }

        if (scenes.Count == 0)
        {
            throw new InvalidOperationException("Ollama returned no scenes.");
        }

        return new GeneratedScriptDto(scenes, raw);
    }

    private static string ReadString(JsonElement item, params string[] names)
    {
        foreach (var name in names)
        {
            if (item.TryGetProperty(name, out var value))
            {
                return value.GetString() ?? "";
            }
        }

        return "";
    }
}
