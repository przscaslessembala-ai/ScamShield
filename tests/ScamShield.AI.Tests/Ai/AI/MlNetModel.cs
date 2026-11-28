using Microsoft.ML;
using ScamShield.Core.Models;

namespace ScamShield.AI;

/// <summary>Charge le modèle ML.NET exporté et calcule un score entre 0 et 1.</summary>
public sealed class MlNetModel
{
    private readonly PredictionEngine<ModelInput, ModelOutput> _engine;
    private readonly object _lock = new(); // PredictionEngine n'est pas thread-safe

    /// <summary>Charge le modèle depuis un fichier .zip.</summary>
    public MlNetModel(string modelPath)
    {
        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException("Modèle IA introuvable.", modelPath);
        }

        var ml = new MLContext();
        var model = ml.Model.Load(modelPath, out _);
        _engine = ml.Model.CreatePredictionEngine<ModelInput, ModelOutput>(model);
    }

    /// <summary>Score entre 0 (sain) et 1 (très dangereux).</summary>
    public double Predict(FeatureVector features)
    {
        var input = ModelInput.From(features);

        ModelOutput output;
        lock (_lock)
        {
            output = _engine.Predict(input);
        }

        if (float.IsNaN(output.Probability))
        {
            throw new InvalidOperationException("Le modèle a rendu un score invalide.");
        }

        return Math.Clamp((double)output.Probability, 0.0, 1.0);
    }

    // Les noms de propriétés doivent être identiques aux colonnes de l'entraînement.
    private sealed class ModelInput
    {
        public float FileSize { get; set; }
        public float Entropy { get; set; }
        public float SectionCount { get; set; }
        public float SuspiciousImportCount { get; set; }
        public float IsSigned { get; set; }
        public float HasPackedSection { get; set; }
        public bool Label { get; set; }

        public static ModelInput From(FeatureVector f) => new()
        {
            FileSize = FeatureAccess.Get(f, FeatureNames.FileSize),
            Entropy = FeatureAccess.Get(f, FeatureNames.Entropy),
            SectionCount = FeatureAccess.Get(f, FeatureNames.SectionCount),
            SuspiciousImportCount = FeatureAccess.Get(f, FeatureNames.SuspiciousImportCount),
            IsSigned = FeatureAccess.Get(f, FeatureNames.IsSigned),
            HasPackedSection = FeatureAccess.Get(f, FeatureNames.HasPackedSection),
        };
    }

    private sealed class ModelOutput
    {
        public float Probability { get; set; }
    }
}