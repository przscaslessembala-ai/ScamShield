namespace ScamShield.Core.Models;

/// <summary>Évaluation du risque d'un fichier par un scorer (modèle IA ou règles).</summary>
public sealed record RiskAssessment
{
    /// <summary>Crée une évaluation.</summary>
    /// <param name="score">Score de risque entre 0 (sain) et 1 (très dangereux).</param>
    /// <param name="reasons">Raisons de ce score, en français simple.</param>
    public RiskAssessment(double score, IReadOnlyList<RiskReason> reasons)
    {
        if (double.IsNaN(score) || score < 0 || score > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(score), score, "Le score doit être compris entre 0 et 1.");
        }

        ArgumentNullException.ThrowIfNull(reasons);

        Score = score;
        Reasons = reasons;
    }

    /// <summary>Score de risque entre 0 et 1.</summary>
    public double Score { get; }

    /// <summary>Raisons du score.</summary>
    public IReadOnlyList<RiskReason> Reasons { get; }
}
