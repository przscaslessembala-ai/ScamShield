using ScamShield.Core.Models;

namespace ScamShield.Core.Abstractions;

/// <summary>
/// Base des menaces connues. Implémentée par ScamShield.Engine (SQLite).
/// </summary>
public interface IThreatRepository
{
    /// <summary>Cherche une menace connue à partir du hash SHA-256 d'un fichier.</summary>
    /// <param name="sha256">Hash SHA-256 en hexadécimal.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>La menace correspondante, ou null si le hash est inconnu.</returns>
    Task<ThreatRecord?> FindBySha256Async(string sha256, CancellationToken cancellationToken = default);

    /// <summary>Ajoute ou met à jour une menace connue.</summary>
    /// <param name="threat">Menace à enregistrer.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    Task AddAsync(ThreatRecord threat, CancellationToken cancellationToken = default);
}
