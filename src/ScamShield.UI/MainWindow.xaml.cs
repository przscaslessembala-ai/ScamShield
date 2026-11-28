// ============================================================================
// MODULE 1 : IMPORTATIONS
// Rôle : Donner accès aux types WPF de base (Window) et à notre ViewModel
//        principal situé dans ScamShield.UI.ViewModels.
// ============================================================================
using System.Windows;
using ScamShield.UI.ViewModels;

// ============================================================================
// MODULE 2 : ESPACE DE NOMS ET CODE-BEHIND DE MAINWINDOW
// Rôle : Initialiser la fenêtre principale et lui assigner une instance de
//        MainViewModel comme DataContext (source de données de la vue).
// ============================================================================
namespace ScamShield.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}