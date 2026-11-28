using ScamShield.Core.Abstractions;
using ScamShield.Core.Models;

namespace ScamShield.AI;

/// <summary>Scorer de risque basé sur le modèle ML.NET, avec explications en français.</summary>
public sealed class MlNetRiskScorer : IRiskScorer
{
    /// <summary>Nom du fichier modèle copié à côté de l'application.</summary>
    public const string DefaultModelFileName = "scamshield-model.zip";

    private readonly MlNetModel _model;
    private readonly RiskExplainer _explainer = new();

    /// <summary>Charge le modèle depuis le dossier de l'application.</summary>
    public MlNetRiskScorer()
        : this(Path.Combine(AppContext.BaseDirectory, DefaultModelFileName))
    {
    }

    /// <summary>Charge le modèle depuis un chemin précis.</summary>
    public MlNetRiskScorer(string modelPath)
    {
        _model = new MlNetModel(modelPath);
    }

    /// <inheritdoc />
    public Task<RiskAssessment> ScoreAsync(FileAnalysis analysis, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        cancellationToken.ThrowIfCancellationRequested();

        var score = _model.Predict(analysis.Features);
        var reasons = _explainer.Explain(analysis.Features);

        return Task.FromResult(new RiskAssessment(score, reasons));
    }
}