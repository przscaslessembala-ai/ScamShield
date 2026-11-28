// ============================================================================
// MODULE 1 : IMPORTATIONS (using) ET ESPACE DE NOMS (namespace)
// Rôle : Charger les outils nécessaires et ranger la classe dans ScamShield.UI.Services.
// ============================================================================

using System;                       // Fournit StringComparison et les bases du langage C#.
using System.IO;                    // Fournit Path.GetFileName pour extraire le nom de fichier depuis un chemin.
using ScamShield.UI.Models;         // Donne accès au modèle ScanResult.

namespace ScamShield.UI.Services    // Même espace de noms que IThreatAnalyzerService et MainViewModel.
{
    // ========================================================================
    // MODULE 2 : SERVICE DE SIMULATION LOCALE (SimulatedThreatAnalyzerService)
    // Rôle : Implémenter IThreatAnalyzerService pour simuler l'analyse d'une cible.
    // ========================================================================

    // public class = classe publique ; : IThreatAnalyzerService = s'engage à implémenter Analyze(string targetPath).
    public class SimulatedThreatAnalyzerService : IThreatAnalyzerService
    {
        // Méthode obligatoire exigée par l'interface IThreatAnalyzerService.
        public ScanResult Analyze(string targetPath)
        {
            // Vérifie si targetPath est null, vide ou composé uniquement d'espaces.
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                // Retourne un objet ScanResult indiquant qu'aucune cible valide n'a été saisie.
                return new ScanResult
                {
                    TargetPath = string.Empty,
                    Status = "Inconnu",
                    RiskScore = 0,
                    Summary = "Aucune cible valide fournie pour la simulation."
                };
            }

            // .Trim() retire les espaces superflus au début et à la fin de la chaîne.
            string cleanTarget = targetPath.Trim();

            // Path.GetFileName extrait le nom du fichier si un chemin complet est fourni (ex. : "C:\dir\test.exe" -> "test.exe").
            string fileName = Path.GetFileName(cleanTarget);

            // Si fileName est vide (ex. : une URL ou un texte sans nom de fichier), on garde cleanTarget.
            string displayTarget = string.IsNullOrWhiteSpace(fileName) ? cleanTarget : fileName;

            // .ToLowerInvariant() met la chaîne en minuscules de façon homogène quel que soit le système.
            string lower = cleanTarget.ToLowerInvariant();

            // Règle 1 : détection de motifs ou extensions dangereuses en simulation locale.
            if (lower.Contains("eicar") ||
                lower.Contains("test-danger") ||
                lower.EndsWith(".exe") ||
                lower.EndsWith(".bat") ||
                lower.EndsWith(".ps1"))
            {
                return new ScanResult
                {
                    TargetPath = cleanTarget,
                    Status = "Analysé (Dangereux)",
                    RiskScore = 90,
                    Summary = $"{displayTarget} : Fichier ou motif à risque élevé détecté lors de la simulation locale."
                };
            }

            // Règle 2 : détection d'indicateurs suspects (scripts, liens http://, mot-clé suspect).
            if (lower.Contains("suspect") ||
                lower.EndsWith(".scr") ||
                lower.EndsWith(".vbs") ||
                lower.StartsWith("http://"))
            {
                return new ScanResult
                {
                    TargetPath = cleanTarget,
                    Status = "Analysé (Suspect)",
                    RiskScore = 55,
                    Summary = $"{displayTarget} : Extension ou comportement suspect détecté lors de la simulation locale."
                };
            }

            // Règle 3 : aucun indicateur dangereux ou suspect repéré -> cible considérée comme saine.
            return new ScanResult
            {
                TargetPath = cleanTarget,
                Status = "Analysé (Sain)",
                RiskScore = 10,
                Summary = $"{displayTarget} : Aucune menace détectée lors de la simulation locale."
            };
        }
    }
}