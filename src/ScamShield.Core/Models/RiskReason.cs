namespace ScamShield.Core.Models;

/// <summary>Une raison pour laquelle un fichier est jugé risqué.</summary>
/// <param name="Code">Code technique stable (par exemple HIGH_ENTROPY).</param>
/// <param name="Description">Explication en français simple, affichée à l'utilisateur.</param>
/// <param name="Weight">Poids de cette raison dans la décision, entre 0 et 1.</param>
public sealed record RiskReason(string Code, string Description, double Weight = 0);
