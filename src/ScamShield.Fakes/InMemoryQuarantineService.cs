using System.Collections.Concurrent;
using ScamShield.Core.Abstractions;
using ScamShield.Core.Models;

namespace ScamShield.Fakes;

/// <summary>Quarantaine en mémoire : ne déplace aucun fichier, pour les tests et les maquettes de l'interface.</summary>
public sealed class InMemoryQuarantineService : IQuarantineService
{
    private readonly ConcurrentDictionary<Guid, QuarantineEntry> _entries = new();

    /// <inheritdoc />
    public Task<QuarantineEntry> QuarantineAsync(ScanResult result, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        var id = Guid.NewGuid();
        var entry = new QuarantineEntry(
            id,
            result.FilePath,
            Path.Combine("quarantine", $"{id:N}.quar"),
            result.Sha256,
            result.Score,
            result.Reasons,
            DateTimeOffset.UtcNow);

        _entries[id] = entry;
        return Task.FromResult(entry);
    }

    /// <inheritdoc />
    public Task RestoreAsync(Guid entryId, CancellationToken cancellationToken = default)
    {
        Remove(entryId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(Guid entryId, CancellationToken cancellationToken = default)
    {
        Remove(entryId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<QuarantineEntry>> ListAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<QuarantineEntry> entries = _entries.Values
            .OrderBy(entry => entry.QuarantinedAt)
            .ToList();

        return Task.FromResult(entries);
    }

    private void Remove(Guid entryId)
    {
        if (!_entries.TryRemove(entryId, out _))
        {
            throw new KeyNotFoundException($"Aucune entrée de quarantaine avec l'identifiant {entryId}.");
        }
    }
}
