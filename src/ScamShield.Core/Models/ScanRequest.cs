namespace ScamShield.Core.Models;

/// <summary>
/// Demande d'analyse d'un fichier. Créée par l'Agent quand un fichier est prêt à être examiné.
/// </summary>
public sealed record ScanRequest
{
    /// <summary>Crée une demande d'analyse.</summary>
    /// <param name="filePath">Chemin complet du fichier à analyser.</param>
    /// <param name="trigger">Origine de la demande.</param>
    /// <param name="requestedAt">Date de la demande (maintenant par défaut, en UTC).</param>
    public ScanRequest(string filePath, ScanTrigger trigger, DateTimeOffset? requestedAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        Id = Guid.NewGuid();
        FilePath = filePath;
        Trigger = trigger;
        RequestedAt = requestedAt ?? DateTimeOffset.UtcNow;
    }

    /// <summary>Identifiant unique de la demande, repris dans le résultat.</summary>
    public Guid Id { get; }

    /// <summary>Chemin complet du fichier à analyser.</summary>
    public string FilePath { get; }

    /// <summary>Origine de la demande.</summary>
    public ScanTrigger Trigger { get; }

    /// <summary>Date de la demande.</summary>
    public DateTimeOffset RequestedAt { get; }
}
