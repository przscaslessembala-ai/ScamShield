namespace ScamShield.Core.Models;

/// <summary>Fichier placé en quarantaine, avec ce qu'il faut pour le restaurer.</summary>
/// <param name="Id">Identifiant de l'entrée.</param>
/// <param name="OriginalPath">Chemin d'origine, utilisé pour la restauration.</param>
/// <param name="StoredPath">Chemin du fichier dans le dossier de quarantaine (renommé, non exécutable).</param>
/// <param name="Sha256">Hash SHA-256 du fichier.</param>
/// <param name="Score">Score de risque ayant justifié la quarantaine.</param>
/// <param name="Reasons">Raisons affichées à l'utilisateur.</param>
/// <param name="QuarantinedAt">Date de mise en quarantaine (UTC).</param>
public sealed record QuarantineEntry(
    Guid Id,
    string OriginalPath,
    string StoredPath,
    string Sha256,
    double Score,
    IReadOnlyList<RiskReason> Reasons,
    DateTimeOffset QuarantinedAt);
