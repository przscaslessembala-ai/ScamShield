using ScamShield.Core.Abstractions;
using ScamShield.Core.Models;

namespace ScamShield.Engine.Scoring;

/// <summary>
/// Plan B : le score des règles d'Andy derrière IRiskScorer.
/// Le trajet complet fonctionne même si le modèle IA n'est pas prêt.
/// </summary>
public sealed class RuleBasedRiskScorer : IRiskScorer
{
    /// <inheritdoc />
    public Task<RiskAssessment> ScoreAsync(FileAnalysis analysis, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(analysis);

        var score = Math.Clamp(analysis.RuleScore / 100.0, 0.0, 1.0);
        return Task.FromResult(new RiskAssessment(score, analysis.RuleReasons));
    }
}
