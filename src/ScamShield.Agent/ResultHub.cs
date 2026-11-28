using System.Collections.Concurrent;
using ScamShield.Core.Models;

namespace ScamShield.Agent;

/// <summary>Résultat d'un fichier traité, avec l'entrée de quarantaine éventuelle.</summary>
public sealed record ScanOutcome(ScanResult Result, QuarantineEntry? Quarantine);

/// <summary>Diffuse les résultats aux clients connectés (named pipe).</summary>
public sealed class ResultHub
{
    public event Action<ScanOutcome>? OutcomePublished;

    public void Publish(ScanOutcome outcome)
    {
        try
        {
            OutcomePublished?.Invoke(outcome);
        }
        catch (Exception)
        {
            // Un abonné défaillant ne doit jamais bloquer l'analyse.
        }
    }
}

/// <summary>
/// Hashs que l'utilisateur a restaurés volontairement : ils ne sont pas remis en quarantaine
/// (sinon la restauration d'un fichier déclencherait aussitôt une nouvelle quarantaine).
/// Mémoire du processus uniquement : la liste blanche persistante viendra avec les paramètres de l'UI.
/// </summary>
public sealed class UserAllowList
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _hashes = new(StringComparer.OrdinalIgnoreCase);

    public void Allow(string sha256) => _hashes[sha256] = DateTimeOffset.UtcNow;

    public bool IsAllowed(string sha256) => _hashes.ContainsKey(sha256);
}
