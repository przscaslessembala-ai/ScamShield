namespace ScamShield.Core.Models;

/// <summary>Classement final d'un fichier analysé.</summary>
public enum Verdict
{
    /// <summary>Aucun indice sérieux : le fichier est laissé en place.</summary>
    Safe = 0,

    /// <summary>Indices inquiétants : l'utilisateur est averti, sans quarantaine automatique.</summary>
    Suspicious = 1,

    /// <summary>Fichier dangereux : mise en quarantaine automatique.</summary>
    Dangerous = 2,
}
