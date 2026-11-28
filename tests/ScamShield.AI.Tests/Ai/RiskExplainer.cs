using ScamShield.Core.Models;

namespace ScamShield.AI;

/// <summary>Traduit les mesures d'un fichier en raisons lisibles, en français simple.</summary>
public sealed class RiskExplainer
{
    /// <summary>Retourne au maximum 3 raisons, les plus importantes d'abord.</summary>
    public IReadOnlyList<RiskReason> Explain(FeatureVector features)
    {
        var entropy = FeatureAccess.Get(features, FeatureNames.Entropy);
        var suspiciousImports = FeatureAccess.Get(features, FeatureNames.SuspiciousImportCount);
        var isSigned = FeatureAccess.Get(features, FeatureNames.IsSigned) >= 0.5f;
        var hasPacked = FeatureAccess.Get(features, FeatureNames.HasPackedSection) >= 0.5f;

        var reasons = new List<RiskReason>();

        if (hasPacked || entropy > 7.0f)
        {
            reasons.Add(new RiskReason(
                AiReasonCodes.PackedOrEncrypted,
                "Le fichier est compressé ou chiffré : c'est typique d'un logiciel qui cache son contenu.",
                0.8));
        }

        if (suspiciousImports > 3)
        {
            reasons.Add(new RiskReason(
                AiReasonCodes.SuspiciousImports,
                "Le fichier utilise des fonctions de Windows souvent employées pour espionner ou modifier le système.",
                0.7));
        }

        if (!isSigned)
        {
            reasons.Add(new RiskReason(
                AiReasonCodes.Unsigned,
                "Le fichier n'a pas de signature numérique : on ne sait pas qui l'a créé.",
                0.3));
        }

        if (isSigned && reasons.Count == 0)
        {
            reasons.Add(new RiskReason(
                AiReasonCodes.SignedClean,
                "Le fichier est signé et ne présente aucun comportement inhabituel.",
                0.0));
        }

        return reasons.OrderByDescending(r => r.Weight).Take(3).ToList();
    }
}