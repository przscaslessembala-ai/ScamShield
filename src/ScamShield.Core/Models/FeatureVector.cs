namespace ScamShield.Core.Models;

/// <summary>
/// Liste des mesures d'un fichier, produite par l'Engine et consommée par l'IA.
/// </summary>
/// <param name="SchemaVersion">Version du schéma, pour détecter un désaccord entre l'Engine et le modèle.</param>
/// <param name="Values">Valeur de chaque caractéristique, indexée par son nom (voir <see cref="FeatureNames"/>).</param>
public sealed record FeatureVector(int SchemaVersion, IReadOnlyDictionary<string, float> Values)
{
    /// <summary>Version actuelle du schéma de caractéristiques.</summary>
    public const int CurrentSchemaVersion = 1;
}
