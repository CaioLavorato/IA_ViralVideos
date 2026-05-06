using Microsoft.Extensions.Options;

namespace VideoSaaS.Infrastructure.Media;

public sealed class MediaPathBuilder(IOptions<MediaOptions> options)
{
    public string GetRootDirectory()
    {
        return Path.GetFullPath(options.Value.RootPath);
    }

    public string GetJobDirectory(Guid tenantId, Guid jobId)
    {
        var root = GetRootDirectory();
        var path = Path.Combine(root, tenantId.ToString("N"), jobId.ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    public string GetDockerJobDirectory(string jobDirectory)
    {
        var root = Path.GetFullPath(options.Value.RootPath);
        var bindRoot = string.IsNullOrWhiteSpace(options.Value.DockerBindRootPath)
            ? root
            : Path.GetFullPath(options.Value.DockerBindRootPath);
        var relative = Path.GetRelativePath(root, jobDirectory);
        return Path.Combine(bindRoot, relative);
    }
}
