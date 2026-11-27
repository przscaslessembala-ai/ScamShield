using ScamShield.Core.Models;

namespace ScamShield.Core.Abstractions;

/// <summary>
/// Gestion de la quarantaine. Implémentée par ScamShield.Agent.
/// </summary>
public interface IQuarantineService
{
    /// <summary>Place en quarantaine le fichier désigné par un résultat d'analyse.</summary>
    /// <param name="result">Résultat d'analyse du fichier dangereux.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    Task<QuarantineEntry> QuarantineAsync(ScanResult result, CancellationToken cancellationToken = default);

    /// <summary>Restaure un fichier à son emplacement d'origine.</summary>
    /// <param name="entryId">Identifiant de l'entrée de quarantaine.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    Task RestoreAsync(Guid entryId, CancellationToken cancellationToken = default);

    /// <summary>Supprime définitivement un fichier en quarantaine.</summary>
    /// <param name="entryId">Identifiant de l'entrée de quarantaine.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    Task DeleteAsync(Guid entryId, CancellationToken cancellationToken = default);

    /// <summary>Liste les fichiers actuellement en quarantaine.</summary>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    Task<IReadOnlyList<QuarantineEntry>> ListAsync(CancellationToken cancellationToken = default);
}
