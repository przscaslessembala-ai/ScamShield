// ============================================================================
// MODULE 1 : IMPORTATIONS (OUTILS .NET POUR LES COMMANDES)
// Rôle : Donner accès à ICommand pour connecter l'interface (View) aux
//        actions du ViewModel, et à ArgumentNullException pour sécuriser.
// ============================================================================
using System.Windows.Input;             // Contient ICommand et EventHandler

// ============================================================================
// MODULE 2 : ESPACE DE NOMS ET DÉCLARATION DE LA CLASSE
// Rôle : Placer RelayCommand dans ScamShield.UI.ViewModels et implémenter
//        l'interface standard ICommand de WPF.
// ============================================================================
namespace ScamShield.UI.ViewModels;

public class RelayCommand : ICommand
{
    // ========================================================================
    // MODULE 3 : CHAMPS PRIVÉS (ACTIONS ET CONDITIONS STOCKÉES)
    // Rôle : Garder en mémoire la méthode à exécuter (_execute) et, si elle
    //        existe, la condition qui autorise ou bloque l'action (_canExecute).
    // ========================================================================
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    // ========================================================================
    // MODULE 4 : CONSTRUCTEUR, ÉVÉNEMENT ET MÉTHODES D'ICommand
    // Rôle : Initialiser la commande, informer WPF quand l'état d'activation
    //        peut changer, vérifier si l'action est possible, et l'exécuter.
    // ========================================================================

    /// <summary>
    /// Initialise une nouvelle commande reliée à une action (execute) et
    /// optionnellement à une condition d'activation (canExecute).
    /// </summary>
    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        ArgumentNullException.ThrowIfNull(execute);
        _execute = execute;
        _canExecute = canExecute;
    }

    /// <summary>
    /// Événement déclenché lorsque la possibilité d'exécuter la commande change
    /// (par exemple : activer ou désactiver un bouton).
    /// </summary>
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <summary>
    /// Indique à l'interface si la commande peut s'exécuter actuellement.
    /// Si aucune condition n'a été fournie, retourne toujours true.
    /// </summary>
    public bool CanExecute(object? parameter)
    {
        return _canExecute is null || _canExecute(parameter);
    }

    /// <summary>
    /// Exécute l'action associée à la commande.
    /// </summary>
    public void Execute(object? parameter)
    {
        _execute(parameter);
    }
}