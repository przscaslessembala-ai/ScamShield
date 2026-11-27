namespace ScamShield.Core.Models;

/// <summary>Origine d'une demande d'analyse.</summary>
public enum ScanTrigger
{
    /// <summary>Analyse lancée manuellement par l'utilisateur.</summary>
    Manual = 0,

    /// <summary>Nouveau fichier détecté dans un dossier surveillé (Téléchargements).</summary>
    Download = 1,

    /// <summary>Fichier présent sur une clé USB ou un disque amovible.</summary>
    RemovableDrive = 2,

    /// <summary>Analyse planifiée.</summary>
    Scheduled = 3,
}
