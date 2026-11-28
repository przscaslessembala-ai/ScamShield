// ============================================================================
// MODULE 1 : IMPORTATIONS (OUTILS .NET UTILISÉS)
// Rôle : Donner accès aux classes standard de .NET pour la notification
//        de changement de propriétés et la détection automatique du nom.
// ============================================================================
using System.ComponentModel;          // Contient INotifyPropertyChanged et PropertyChangedEventArgs
using System.Runtime.CompilerServices;  // Contient [CallerMemberName] pour obtenir le nom de l'appelant

// ============================================================================
// MODULE 2 : ESPACE DE NOMS ET DÉFINITION DE LA CLASSE DE BASE
// Rôle : Organiser le code dans le dossier ViewModels et créer un modèle
//        commun dont hériteront tous les ViewModels de l'application.
// ============================================================================
namespace ScamShield.UI.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    // ========================================================================
    // MODULE 3 : ÉVÉNEMENT DE NOTIFICATION VERS L'INTERFACE (VIEW)
    // Rôle : Publier un événement que l'écran XAML écoute pour se rafraîchir
    //        automatiquement lorsqu'une valeur change.
    // ========================================================================
    public event PropertyChangedEventHandler? PropertyChanged;

    // ========================================================================
    // MODULE 4 : MÉTHODES D'AIDE (NOTIFICATION ET MISE À JOUR SÉCURISÉE)
    // Rôle : Centraliser la logique de changement de valeur et d'envoi du
    //        signal afin de garder les autres ViewModels courts et propres.
    // ========================================================================

    /// <summary>
    /// Déclenche l'événement PropertyChanged pour prévenir la vue qu'une propriété a changé.
    /// </summary>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Met à jour un champ uniquement si la nouvelle valeur est différente de l'ancienne,
    /// puis notifie l'interface. Retourne true si la valeur a changé, false sinon.
    /// </summary>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}