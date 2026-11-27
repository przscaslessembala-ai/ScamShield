namespace ScamShield.Core.Models;

/// <summary>
/// Résultat de l'analyse classique (hash, structure, règles) réalisée par l'Engine.
/// </summary>
/// <param name="Request">Demande d'origine.</param>
/// <param name="Sha256">Hash SHA-256 du fichier, en hexadécimal.</param>
/// <param name="SizeBytes">Taille du fichier en octets.</param>
/// <param name="IsKnownThreat">Vrai si le hash figure dans la base des menaces connues.</param>
/// <param name="RuleScore">Total des points du moteur de règles, de 0 à 100.</param>
/// <param name="RuleReasons">Indicateurs suspects relevés par les règles.</param>
/// <param name="Features">Mesures du fichier destinées à l'IA.</param>
public sealed record FileAnalysis(
    ScanRequest Request,
    string Sha256,
    long SizeBytes,
    bool IsKnownThreat,
    int RuleScore,
    IReadOnlyList<RiskReason> RuleReasons,
    FeatureVector Features);
