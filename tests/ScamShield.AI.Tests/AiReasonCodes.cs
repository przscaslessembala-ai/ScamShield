namespace ScamShield.AI;

/// <summary>Codes techniques stables des raisons produites par l'IA.</summary>
public static class AiReasonCodes
{
    /// <summary>Contenu compressé ou chiffré.</summary>
    public const string PackedOrEncrypted = "PACKED_OR_ENCRYPTED";

    /// <summary>Utilisation de fonctions Windows suspectes.</summary>
    public const string SuspiciousImports = "SUSPICIOUS_IMPORTS";

    /// <summary>Pas de signature numérique.</summary>
    public const string Unsigned = "UNSIGNED";

    /// <summary>Fichier signé sans comportement inhabituel.</summary>
    public const string SignedClean = "SIGNED_CLEAN";
}