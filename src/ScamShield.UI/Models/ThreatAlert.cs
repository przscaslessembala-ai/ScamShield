// ============================================================================
// MODULE 1 : IMPORTATIONS
// Rôle : Importer System pour utiliser les types de base et DateTime.
// ============================================================================
using System;

// ============================================================================
// MODULE 2 : ESPACE DE NOMS ET DÉCLARATION DE LA CLASSE THREATALERT
// Rôle : Représenter une alerte lisible pour l'utilisateur (cible, score,
//        zone de risque en français simple, raison et action conseillée).
// ============================================================================
namespace ScamShield.UI.Models;

public class ThreatAlert
{
    // ========================================================================
    // MODULE 3 : PROPRIÉTÉS (ENCAPSULATION DES DONNÉES D'UNE ALERTE)
    // Rôle : Stocker les informations essentielles affichées dans l'interface
    //        sans jargon technique complexe.
    // ========================================================================
    public string TargetName { get; set; } = string.Empty;
    public int RiskScorePercent { get; set; }
    public string RiskZone { get; set; } = "Sain";
    public string Reason { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; } = DateTime.Now;

    // ========================================================================
    // MODULE 4 : CONSTRUCTEUR AVEC VALIDATION ET CALCUL DE ZONE
    // Rôle : Initialiser une alerte proprement, borne le score entre 0 et 100
    //        et détermine automatiquement la zone (Sain / Suspect / Dangereux).
    // ========================================================================
    public ThreatAlert(string targetName, int riskScorePercent, string reason)
    {
        // Validation simple de l'entrée (comme vu sur les chaînes et conditions)
        if (string.IsNullOrWhiteSpace(targetName))
        {
            TargetName = "Élément non nommé";
        }
        else
        {
            TargetName = targetName.Trim();
        }

        // Borner le score entre 0 et 100 avec une structure conditionnelle
        if (riskScorePercent < 0)
        {
            RiskScorePercent = 0;
        }
        else if (riskScorePercent > 100)
        {
            RiskScorePercent = 100;
        }
        else
        {
            RiskScorePercent = riskScorePercent;
        }

        Reason = string.IsNullOrWhiteSpace(reason)
            ? "Aucune explication détaillée fournie pour le moment."
            : reason.Trim();

        // Déterminer la zone selon les 3 niveaux du projet : Sain / Suspect / Dangereux
        if (RiskScorePercent >= 70)
        {
            RiskZone = "Dangereux";
            RecommendedAction = "Mettre en quarantaine ou supprimer si l'origine est inconnue.";
        }
        else if (RiskScorePercent >= 35)
        {
            RiskZone = "Suspect";
            RecommendedAction = "Vérifier l'origine du fichier avant de l'ouvrir.";
        }
        else
        {
            RiskZone = "Sain";
            RecommendedAction = "Aucune action particulière requise.";
        }
    }

    // Constructeur sans paramètre utile pour l'initialisation simple
    public ThreatAlert() : this("Aucun élément", 0, "En attente d'analyse.")
    {
    }
}