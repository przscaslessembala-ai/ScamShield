using ScamShield.Core.Models;

namespace ScamShield.AI;

/// <summary>Lecture sécurisée des mesures d'un <see cref="FeatureVector"/>.</summary>
internal static class FeatureAccess
{
    public static float Get(FeatureVector features, string name)
    {
        ArgumentNullException.ThrowIfNull(features);

        if (features.SchemaVersion != FeatureVector.CurrentSchemaVersion)
        {
            throw new InvalidOperationException(
                $"Schéma de caractéristiques {features.SchemaVersion} non pris en charge " +
                $"(attendu : {FeatureVector.CurrentSchemaVersion}).");
        }

        if (!features.Values.TryGetValue(name, out var value))
        {
            throw new InvalidOperationException($"Caractéristique manquante : {name}.");
        }

        return value;
    }
}