namespace ScamShield.Core.Models;

/// <summary>Menace connue, enregistrée dans la base de l'Engine.</summary>
/// <param name="Sha256">Hash SHA-256 de la menace, en hexadécimal.</param>
/// <param name="Name">Nom de la menace (par exemple EICAR-Test-File).</param>
/// <param name="Source">Origine de l'information (liste publique, ajout manuel...).</param>
/// <param name="AddedAt">Date d'ajout à la base.</param>
public sealed record ThreatRecord(string Sha256, string Name, string Source, DateTimeOffset AddedAt);
