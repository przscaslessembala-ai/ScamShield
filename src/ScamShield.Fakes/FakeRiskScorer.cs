using ScamShield.Core.Abstractions;
using ScamShield.Core.Models;

namespace ScamShield.Fakes;

/// <summary>
/// Scorer factice : reprend simplement le score des règles (0 à 100) et le ramène entre 0 et 1.
/// Sert aussi de modèle pour le scorer de secours à base de règles.
/// </summary>
public sealed class FakeRiskScorer : IRiskScorer
{
    /// <inheritdoc />
    public Task<RiskAssessment> ScoreAsync(FileAnalysis analysis, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(analysis);

        var score = Math.Clamp(analysis.RuleScore / 100.0, 0.0, 1.0);
        return Task.FromResult(new RiskAssessment(score, analysis.RuleReasons));
    }
}
