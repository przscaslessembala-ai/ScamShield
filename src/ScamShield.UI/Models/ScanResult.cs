// ============================================================================
// MODULE 1 : ESPACE DE NOMS ET DÉCLARATION DU MODÈLE
// Rôle : Représenter les données d'un résultat d'analyse (fichier/cible,
//        niveau de risque, état et résumé) dans le dossier Models.
// ============================================================================
namespace ScamShield.UI.Models;

public class ScanResult
{
    // ========================================================================
    // MODULE 2 : PROPRIÉTÉS DE DONNÉES
    // Rôle : Stocker les informations descriptives et le score d'analyse.
    // ========================================================================
    public string TargetPath { get; set; } = string.Empty;
    public string Status { get; set; } = "En attente";
    public int RiskScore { get; set; }
    public string Summary { get; set; } = string.Empty;
}