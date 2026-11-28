using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ScamShield.UI.ViewModels
{
    public sealed class TroubleshootingScenario
    {
        public string Title { get; init; } = string.Empty;
        public string QuickSummary { get; init; } = string.Empty;
        public string RecommendedActionLabel { get; init; } = string.Empty;
        public string ActionKey { get; init; } = string.Empty;
    }

    public sealed class TroubleshootingViewModel : INotifyPropertyChanged
    {
        private TroubleshootingScenario? _selectedScenario;
        private string _statusMessage = "Choisissez une situation pour voir quoi faire en quelques secondes.";

        public TroubleshootingViewModel()
        {
            Scenarios = new ObservableCollection<TroubleshootingScenario>
            {
                new()
                {
                    Title = "Fichier dangereux ou suspect signalé",
                    QuickSummary = "Un fichier a montré un comportement risqué : il est isolé ou signalé pour éviter tout dommage. Vérifiez la quarantaine et ne lancez pas le fichier.",
                    RecommendedActionLabel = "Ouvrir la quarantaine",
                    ActionKey = "quarantine"
                },
                new()
                {
                    Title = "Doute sur un faux positif (fichier légitime)",
                    QuickSummary = "Un fichier de confiance a peut-être été bloqué à tort. Vérifiez d'abord son origine avant de le restaurer ou d'ajouter son dossier en liste blanche.",
                    RecommendedActionLabel = "Ouvrir les paramètres",
                    ActionKey = "settings"
                },
                new()
                {
                    Title = "Service d'analyse ou liaison indisponible",
                    QuickSummary = "L'interface ne reçoit pas de réponse immédiate du service. Relancez une vérification d'état ou réessayez la connexion.",
                    RecommendedActionLabel = "Tester l'état du service",
                    ActionKey = "service"
                },
                new()
                {
                    Title = "Vérification après changement de paramètres",
                    QuickSummary = "Vous avez modifié la sensibilité ou les dossiers surveillés. Actualisez l'état pour confirmer que la protection est active.",
                    RecommendedActionLabel = "Actualiser l'état",
                    ActionKey = "refresh"
                }
            };

            _selectedScenario = Scenarios[0];
            RunRecommendedActionCommand = new RelayCommand(ExecuteRecommendedAction);
        }

        public ObservableCollection<TroubleshootingScenario> Scenarios { get; }

        public TroubleshootingScenario? SelectedScenario
        {
            get => _selectedScenario;
            set
            {
                if (!Equals(_selectedScenario, value))
                {
                    _selectedScenario = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand RunRecommendedActionCommand { get; }

        public event Action<string>? ActionRequested;
        public event PropertyChangedEventHandler? PropertyChanged;

        private void ExecuteRecommendedAction()
        {
            if (SelectedScenario is null)
            {
                StatusMessage = "Aucune situation sélectionnée.";
                return;
            }

            StatusMessage = $"Action déclenchée : {SelectedScenario.RecommendedActionLabel}.";
            ActionRequested?.Invoke(SelectedScenario.ActionKey);
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private sealed class RelayCommand : ICommand
        {
            private readonly Action _execute;

            public RelayCommand(Action execute)
            {
                _execute = execute;
            }

            public event EventHandler? CanExecuteChanged
            {
                add { }
                remove { }
            }

            public bool CanExecute(object? parameter) => true;

            public void Execute(object? parameter) => _execute();
        }
    }
}