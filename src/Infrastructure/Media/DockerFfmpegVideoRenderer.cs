using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VideoSaaS.Application.Abstractions;
using VideoSaaS.Domain.Entities;
using VideoSaaS.Domain.ValueObjects;

namespace VideoSaaS.Infrastructure.Media;

public sealed class DockerFfmpegVideoRenderer(
    IOptions<MediaOptions> options,
    MediaPathBuilder paths,
    ILogger<DockerFfmpegVideoRenderer> logger) : IVideoRenderer
{
    public async Task<RenderedVideoResult> RenderAsync(VideoJob job, IReadOnlyList<string> audioFiles, CancellationToken cancellationToken)
    {
        var jobDir = paths.GetJobDirectory(job.TenantId, job.Id);
        var listPath = Path.Combine(jobDir, "concat.txt");
        var narrationPath = Path.Combine(jobDir, "narration.wav");
        var videoPath = Path.Combine(jobDir, "video.mp4");
        var captionsPath = Path.Combine(jobDir, "captions.ass");
        var preset = VideoFormatPreset.FromCode(job.Format);
        var reelPath = Path.Combine(jobDir, preset.FinalFileName);

        await BuildConcatListAsync(job, listPath, cancellationToken);
        await BuildCaptionsAsync(job, captionsPath, preset, cancellationToken);
        var dockerJobDir = paths.GetDockerJobDirectory(jobDir);
        await MergeAudioAsync(jobDir, dockerJobDir, audioFiles, narrationPath, cancellationToken);
        await RenderConcatAsync(dockerJobDir, videoPath, preset, cancellationToken);
        await RenderFinalAsync(dockerJobDir, videoPath, narrationPath, captionsPath, reelPath, preset, cancellationToken);

        logger.LogInformation("Rendered video job {JobId} to {ReelPath}", job.Id, reelPath);
        return new RenderedVideoResult(videoPath, reelPath, narrationPath);
    }

    private static async Task BuildConcatListAsync(VideoJob job, string listPath, CancellationToken cancellationToken)
    {
        var lines = job.Scenes.SelectMany(scene => new[]
        {
            $"file '{Path.GetFileName(scene.ImagePath!)}'",
            $"duration {scene.EstimatedSeconds:0.###}"
        }).ToList();
        lines.Add($"file '{Path.GetFileName(job.Scenes.Last().ImagePath!)}'");
        await File.WriteAllLinesAsync(listPath, lines, cancellationToken);
        
        // Workaround for Docker Desktop (WSL2) file synchronization delay on Windows
        await Task.Delay(1500, cancellationToken);
    }

    private static async Task BuildCaptionsAsync(VideoJob job, string captionsPath, VideoFormatPreset preset, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[Script Info]");
        sb.AppendLine("ScriptType: v4.00+");
        sb.AppendLine("WrapStyle: 2");
        sb.AppendLine("ScaledBorderAndShadow: yes");
        sb.AppendLine($"PlayResX: {preset.Width}");
        sb.AppendLine($"PlayResY: {preset.Height}");
        sb.AppendLine();
        sb.AppendLine("[V4+ Styles]");
        sb.AppendLine("Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding");
        var fontSize = preset.Code.Contains("9_16", StringComparison.OrdinalIgnoreCase) ? 62 : 48;
        var marginV = preset.Code.Contains("9_16", StringComparison.OrdinalIgnoreCase) ? 190 : 80;
        sb.AppendLine($"Style: Caption,Arial,{fontSize},&H00FFFFFF,&H0000E6FF,&H00201910,&HAA000000,-1,0,0,0,100,100,0,0,3,4,1,2,80,80,{marginV},1");
        sb.AppendLine("[Events]");
        sb.AppendLine("Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text");

        var current = TimeSpan.Zero;
        foreach (var scene in job.Scenes.OrderBy(scene => scene.Index))
        {
            var duration = TimeSpan.FromSeconds(Math.Max(2.5, scene.EstimatedSeconds));
            var chunks = SplitCaption(scene.Text);
            var chunkDuration = TimeSpan.FromSeconds(duration.TotalSeconds / chunks.Count);

            for (var i = 0; i < chunks.Count; i++)
            {
                var start = current + TimeSpan.FromSeconds(i * chunkDuration.TotalSeconds);
                var end = i == chunks.Count - 1 ? current + duration : start + chunkDuration;
                var text = EscapeAss(chunks[i]);
                sb.AppendLine($"Dialogue: 0,{FormatAssTime(start)},{FormatAssTime(end)},Caption,,0,0,0,,{{\\u1\\bord4\\shad1\\blur0.5}}{text}");
            }

            current += duration;
        }

        await File.WriteAllTextAsync(captionsPath, sb.ToString(), Encoding.UTF8, cancellationToken);
    }

    private static List<string> SplitCaption(string text)
    {
        var words = text
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var chunks = new List<string>();
        var current = new StringBuilder();
        foreach (var word in words)
        {
            if (current.Length > 0 && current.Length + word.Length > 34)
            {
                chunks.Add(current.ToString());
                current.Clear();
            }

            if (current.Length > 0)
            {
                current.Append(' ');
            }

            current.Append(word);
        }

        if (current.Length > 0)
        {
            chunks.Add(current.ToString());
        }

        return chunks.Count > 0 ? chunks : [text];
    }

    private static string EscapeAss(string text)
    {
        return text
            .Replace(@"\", @"\\")
            .Replace("{", "\\{")
            .Replace("}", "\\}")
            .Replace("\r", " ")
            .Replace("\n", " ");
    }

    private static string FormatAssTime(TimeSpan time)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}.{time.Milliseconds / 10:00}");
    }

    private async Task MergeAudioAsync(string jobDir, string dockerJobDir, IReadOnlyList<string> audioFiles, string outputPath, CancellationToken cancellationToken)
    {
        if (audioFiles.Count == 0)
        {
            throw new InvalidOperationException("Nenhum áudio foi gerado para este job.");
        }

        foreach (var audioFile in audioFiles)
        {
            var fullAudioPath = Path.GetFullPath(audioFile);
            if (!File.Exists(fullAudioPath))
            {
                throw new FileNotFoundException("Áudio gerado não encontrado antes da concatenação.", fullAudioPath);
            }

            if (!Path.GetFullPath(jobDir).Equals(Path.GetDirectoryName(fullAudioPath), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Áudio gerado fora do diretório do job: {fullAudioPath}");
            }
        }

        var inputs = audioFiles.Select(p => $"-i /work/{Path.GetFileName(p)}");
        var filter = $"concat=n={audioFiles.Count}:v=0:a=1[outa]";
        var args = $"""run --rm -v "{dockerJobDir}:/work" {options.Value.FfmpegImage} -y {string.Join(' ', inputs)} -filter_complex "{filter}" -map "[outa]" /work/{Path.GetFileName(outputPath)}""";
        await RunProcessAsync("docker", args, "FFmpeg audio concat", cancellationToken);
    }

    private async Task RenderConcatAsync(string dockerJobDir, string outputPath, VideoFormatPreset preset, CancellationToken cancellationToken)
    {
        var args = $"""run --rm -v "{dockerJobDir}:/work" {options.Value.FfmpegImage} -y -f concat -safe 0 -i /work/concat.txt -vf "scale={preset.Width}:{preset.Height}:force_original_aspect_ratio=increase,crop={preset.Width}:{preset.Height},format=yuv420p" -c:v libx264 -preset veryfast -r 30 /work/{Path.GetFileName(outputPath)}""";
        await RunProcessAsync("docker", args, "FFmpeg concat video", cancellationToken);
    }

    private async Task RenderFinalAsync(string dockerJobDir, string videoPath, string audioPath, string captionsPath, string reelPath, VideoFormatPreset preset, CancellationToken cancellationToken)
    {
        var args = $"""run --rm -v "{dockerJobDir}:/work" {options.Value.FfmpegImage} -y -i /work/{Path.GetFileName(videoPath)} -i /work/{Path.GetFileName(audioPath)} -vf "scale={preset.Width}:{preset.Height}:force_original_aspect_ratio=increase,crop={preset.Width}:{preset.Height},subtitles=/work/{Path.GetFileName(captionsPath)},format=yuv420p" -c:v libx264 -preset veryfast -c:a aac -shortest /work/{Path.GetFileName(reelPath)}""";
        await RunProcessAsync("docker", args, $"FFmpeg {preset.Label} render", cancellationToken);
    }

    private async Task RunProcessAsync(string fileName, string arguments, string operation, CancellationToken cancellationToken)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
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
