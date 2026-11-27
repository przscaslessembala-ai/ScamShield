namespace ScamShield.Core.Models;

/// <summary>Résultat final d'une analyse, affiché par l'interface et utilisé par l'Agent.</summary>
/// <param name="RequestId">Identifiant de la demande d'origine.</param>
/// <param name="FilePath">Chemin du fichier analysé.</param>
/// <param name="Sha256">Hash SHA-256 du fichier.</param>
/// <param name="Verdict">Verdict final.</param>
/// <param name="Score">Score de risque entre 0 et 1.</param>
/// <param name="Reasons">Raisons du verdict, en français simple.</param>
/// <param name="CompletedAt">Date de fin de l'analyse (UTC).</param>
public sealed record ScanResult(
    Guid RequestId,
    string FilePath,
    string Sha256,
    Verdict Verdict,
    double Score,
    IReadOnlyList<RiskReason> Reasons,
    DateTimeOffset CompletedAt)
{
    /// <summary>
    /// Assemble le résultat final à partir de l'analyse classique et de l'évaluation du risque.
    /// Une menace connue est toujours jugée dangereuse, quel que soit le score.
    /// </summary>
    /// <param name="analysis">Analyse de l'Engine.</param>
    /// <param name="assessment">Évaluation du scorer.</param>
    /// <param name="policy">Seuils à appliquer (seuils par défaut si absent).</param>
    /// <param name="clock">Horloge, remplaçable dans les tests (horloge système si absent).</param>
    public static ScanResult Create(
        FileAnalysis analysis,
        RiskAssessment assessment,
        VerdictPolicy? policy = null,
        TimeProvider? clock = null)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(assessment);

        policy ??= VerdictPolicy.Default;
        clock ??= TimeProvider.System;

        var reasons = new List<RiskReason>();
        var score = assessment.Score;

        if (analysis.IsKnownThreat)
        {
            score = 1.0;
            reasons.Add(new RiskReason(
                RiskReasonCodes.KnownThreat,
                "Ce fichier correspond à une menace déjà connue.",
                1.0));
        }

        reasons.AddRange(assessment.Reasons);

        var verdict = analysis.IsKnownThreat ? Verdict.Dangerous : policy.Evaluate(score);

        return new ScanResult(
            analysis.Request.Id,
            analysis.Request.FilePath,
            analysis.Sha256,
            verdict,
            score,
            reasons,
            clock.GetUtcNow());
    }
}
