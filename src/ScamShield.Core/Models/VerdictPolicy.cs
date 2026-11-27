namespace ScamShield.Core.Models;

/// <summary>
/// Règle qui transforme un score de risque en verdict. Centralisée ici pour que l'Agent,
/// l'interface et les tests appliquent exactement les mêmes seuils.
/// </summary>
public sealed record VerdictPolicy
{
    /// <summary>Seuils par défaut : suspect à partir de 0,5, dangereux à partir de 0,8.</summary>
    public static VerdictPolicy Default { get; } = new(0.5, 0.8);

    /// <summary>Crée une politique de verdict.</summary>
    /// <param name="suspiciousThreshold">Score à partir duquel un fichier est suspect.</param>
    /// <param name="dangerousThreshold">Score à partir duquel un fichier est dangereux.</param>
    public VerdictPolicy(double suspiciousThreshold, double dangerousThreshold)
    {
        var valid = suspiciousThreshold > 0
            && suspiciousThreshold < dangerousThreshold
            && dangerousThreshold <= 1;

        if (!valid)
        {
            throw new ArgumentOutOfRangeException(
                nameof(suspiciousThreshold),
                suspiciousThreshold,
                "Les seuils doivent respecter : 0 < suspect < dangereux <= 1.");
        }

        SuspiciousThreshold = suspiciousThreshold;
        DangerousThreshold = dangerousThreshold;
    }

    /// <summary>Score à partir duquel un fichier est suspect.</summary>
    public double SuspiciousThreshold { get; }

    /// <summary>Score à partir duquel un fichier est dangereux.</summary>
    public double DangerousThreshold { get; }

    /// <summary>Convertit un score en verdict.</summary>
    /// <param name="score">Score de risque entre 0 et 1.</param>
    public Verdict Evaluate(double score)
    {
        if (double.IsNaN(score) || score < 0 || score > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(score), score, "Le score doit être compris entre 0 et 1.");
        }

        if (score >= DangerousThreshold)
        {
            return Verdict.Dangerous;
        }

        return score >= SuspiciousThreshold ? Verdict.Suspicious : Verdict.Safe;
    }
}
