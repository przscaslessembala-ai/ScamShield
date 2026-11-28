namespace ScamShield.Engine.Analysis;

/// <summary>
/// Codes des raisons produites par le moteur de règles.
/// À proposer plus tard à Prisca pour les rapatrier dans RiskReasonCodes (Core), via une pull request.
/// </summary>
public static class EngineReasonCodes
{
    /// <summary>Entropie très élevée : contenu compressé ou chiffré.</summary>
    public const string HighEntropy = "HIGH_ENTROPY";

    /// <summary>Une section de l'exécutable ressemble à du contenu compressé ou chiffré.</summary>
    public const string PackedSection = "PACKED_SECTION";

    /// <summary>L'exécutable importe des fonctions Windows souvent détournées.</summary>
    public const string SuspiciousImports = "SUSPICIOUS_IMPORTS";

    /// <summary>Exécutable sans signature numérique valide.</summary>
    public const string UnsignedExecutable = "UNSIGNED_EXECUTABLE";

    /// <summary>Nom de fichier du type « facture.pdf.exe ».</summary>
    public const string DoubleExtension = "DOUBLE_EXTENSION";

    /// <summary>Fichier autorun.inf (exécution automatique depuis un support amovible).</summary>
    public const string AutorunFile = "AUTORUN_FILE";

    /// <summary>Économiseur d'écran (.scr) : en réalité un exécutable.</summary>
    public const string ScreensaverExecutable = "SCREENSAVER_EXECUTABLE";
}
