using System.Threading.Channels;
using VideoSaaS.Application.Abstractions;

namespace VideoSaaS.Infrastructure.Queue;

public sealed class InMemoryJobQueue : IJobQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
    {
        SingleReader = false,
        SingleWriter = false
    });

    public ValueTask EnqueueAsync(Guid jobId, CancellationToken cancellationToken) => _channel.Writer.WriteAsync(jobId, cancellationToken);
    public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken) => _channel.Reader.ReadAsync(cancellationToken);
}
