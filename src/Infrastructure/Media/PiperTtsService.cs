using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VideoSaaS.Application.Abstractions;
using VideoSaaS.Domain.ValueObjects;

namespace VideoSaaS.Infrastructure.Media;

public sealed class PiperTtsService(
    IOptions<MediaOptions> options,
    MediaPathBuilder paths,
    ILogger<PiperTtsService> logger) : ITtsService
{
    public async Task<string> GenerateSceneAudioAsync(Guid tenantId, Guid jobId, SceneSpec scene, string voice, CancellationToken cancellationToken)
    {
        var jobDir = paths.GetJobDirectory(tenantId, jobId);
        Directory.CreateDirectory(jobDir);

        var audioPath = Path.Combine(jobDir, $"scene-{scene.Index:00}.wav");
        var dockerJobDir = paths.GetDockerJobDirectory(jobDir);

        var modelName = voice.EndsWith(".onnx", StringComparison.OrdinalIgnoreCase) ? voice : $"{voice}.onnx";
        var safeText = scene.Text.Replace("'", "'\"'\"'");
        var command = $"printf '%s' '{safeText}' | piper -m /models/{modelName} -f /work/{Path.GetFileName(audioPath)}";
        var args = new[]
        {
            "run", "--rm", "--entrypoint", "sh",
            "-v", $"{dockerJobDir}:/work",
            "-v", $"{Path.GetFullPath(options.Value.PiperModelsPath)}:/models",
            options.Value.PiperImage,
            "-c", command
        };

        logger.LogInformation("JobDir: {JobDir}", jobDir);
        logger.LogInformation("DockerJobDir: {DockerJobDir}", dockerJobDir);
        logger.LogInformation("PiperModelsPath: {PiperModelsPath}", Path.GetFullPath(options.Value.PiperModelsPath));
        logger.LogInformation("Docker args: {Args}", string.Join(" ", args));
        await RunProcessAsync("docker", args, "Piper TTS", cancellationToken);
        logger.LogInformation("Generated audio for scene {SceneIndex}: {AudioPath}", scene.Index, audioPath);
        return audioPath;
    }

    private async Task RunProcessAsync(string fileName, IEnumerable<string> arguments, string operation, CancellationToken cancellationToken)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo(fileName)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        foreach (var arg in arguments) process.StartInfo.ArgumentList.Add(arg);
        
        process.Start();
        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"{operation} failed with exit code {process.ExitCode}: {stderr} {stdout}");
        }
    }
}
