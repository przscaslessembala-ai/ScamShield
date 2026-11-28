using System.Windows;
using ScamShield.UI.ViewModels;

namespace ScamShield.UI;

public partial class MainWindow : Window
{
    private bool _navigationOpened;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        Loaded += OnMainWindowLoaded;
    }

    private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (_navigationOpened)
        {
            return;
        }

        _navigationOpened = true;

        var navigationWindow = new NavigationWindow();
        navigationWindow.Show();
        navigationWindow.Activate();
    }
}