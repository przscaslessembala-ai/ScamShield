// ============================================================================
// MODULE 1 : IMPORTATIONS ET ESPACE DE NOMS
// Rôle : Charger les modèles et placer le contrat dans ScamShield.UI.Services.
// ============================================================================

using ScamShield.UI.Models;         // Donne accès au type ScanResult retourné par l'analyse.

namespace ScamShield.UI.Services    // Même espace de noms que SimulatedThreatAnalyzerService et MainViewModel.
{
    // ========================================================================
    // MODULE 2 : CONTRAT DU SERVICE D'ANALYSE (IThreatAnalyzerService)
    // Rôle : Définir la méthode obligatoire que tout analyseur doit fournir.
    // ========================================================================

    // public interface = contrat accessible publiquement par les classes public du projet (corrige CS0060 et CS0051).
    public interface IThreatAnalyzerService
    {
        // Déclare la méthode Analyze qui reçoit une cible (string) et renvoie un ScanResult (corrige CS1061).
        ScanResult Analyze(string targetPath);
    }
}