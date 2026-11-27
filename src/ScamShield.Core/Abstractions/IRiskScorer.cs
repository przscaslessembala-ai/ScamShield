using ScamShield.Core.Models;

namespace ScamShield.Core.Abstractions;

/// <summary>
/// Calcule un score de risque et ses raisons à partir d'une analyse.
/// Implémentée par ScamShield.AI (modèle ML.NET) et par un scorer de secours à base de règles.
/// </summary>
public interface IRiskScorer
{
    /// <summary>Évalue le risque d'un fichier déjà analysé.</summary>
    /// <param name="analysis">Résultat de l'analyse classique.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    Task<RiskAssessment> ScoreAsync(FileAnalysis analysis, CancellationToken cancellationToken = default);
}
