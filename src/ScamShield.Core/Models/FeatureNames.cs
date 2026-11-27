namespace ScamShield.Core.Models;

/// <summary>
/// Noms des caractéristiques échangées entre l'Engine (Andy) et l'IA (Gabrielle).
/// Cette liste est une proposition de départ : tout ajout ou retrait passe par une pull request
/// et incrémente <see cref="FeatureVector.CurrentSchemaVersion"/>.
/// </summary>
public static class FeatureNames
{
    /// <summary>Taille du fichier en octets.</summary>
    public const string FileSize = "file_size";

    /// <summary>Entropie de Shannon du fichier (0 à 8).</summary>
    public const string Entropy = "entropy";

    /// <summary>Nombre de sections de l'exécutable (PE).</summary>
    public const string SectionCount = "section_count";

    /// <summary>Nombre d'imports considérés comme suspects.</summary>
    public const string SuspiciousImportCount = "suspicious_import_count";

    /// <summary>1 si le fichier porte une signature numérique valide, sinon 0.</summary>
    public const string IsSigned = "is_signed";

    /// <summary>1 si une section ressemble à du contenu compressé ou chiffré, sinon 0.</summary>
    public const string HasPackedSection = "has_packed_section";

    /// <summary>Toutes les caractéristiques du schéma courant.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        FileSize,
        Entropy,
        SectionCount,
        SuspiciousImportCount,
        IsSigned,
        HasPackedSection,
    ];
}
