using System.Collections.Concurrent;
using ScamShield.Core.Abstractions;
using ScamShield.Core.Models;

namespace ScamShield.Fakes;

/// <summary>Base de menaces en mémoire, sans SQLite, pour les tests et les maquettes.</summary>
public sealed class InMemoryThreatRepository : IThreatRepository
{
    private readonly ConcurrentDictionary<string, ThreatRecord> _threats = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public Task<ThreatRecord?> FindBySha256Async(string sha256, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sha256);

        _threats.TryGetValue(sha256, out var threat);
        return Task.FromResult(threat);
    }

    /// <inheritdoc />
    public Task AddAsync(ThreatRecord threat, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(threat);

        _threats[threat.Sha256] = threat;
        return Task.CompletedTask;
    }
}
