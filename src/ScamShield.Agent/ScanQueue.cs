using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using ScamShield.Core.Models;

namespace ScamShield.Agent;

/// <summary>File d'attente bornée des fichiers à analyser, avec dédoublonnage par chemin.</summary>
public sealed class ScanQueue
{
    private readonly Channel<ScanRequest> _channel;
    private readonly ConcurrentDictionary<string, byte> _pending = new(StringComparer.OrdinalIgnoreCase);

    public ScanQueue(IOptions<AgentOptions> options)
    {
        var capacity = Math.Max(1, options.Value.QueueCapacity);
        _channel = Channel.CreateBounded<ScanRequest>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
        });
    }

    /// <summary>Réserve un chemin ; faux s'il est déjà en cours de traitement.</summary>
    public bool TryReserve(string path) => _pending.TryAdd(path, 0);

    /// <summary>Libère un chemin une fois traité (ou abandonné).</summary>
    public void Release(string path) => _pending.TryRemove(path, out _);

    public ValueTask EnqueueAsync(ScanRequest request, CancellationToken cancellationToken) =>
        _channel.Writer.WriteAsync(request, cancellationToken);

    public IAsyncEnumerable<ScanRequest> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
