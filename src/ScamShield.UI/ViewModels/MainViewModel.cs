// ============================================================================
// MODULE 1 : IMPORTATIONS (using) ET ESPACE DE NOMS (namespace)
// Rôle : Charger les bibliothèques nécessaires et ranger la classe au bon endroit.
// ============================================================================

using System;                                // Contient les bases du langage (ex. : DateTime, Exception, ArgumentNullException, StringComparison).
using System.Collections.ObjectModel;        // Contient ObservableCollection<T>, une liste qui prévient l'écran quand on ajoute/retire un élément.
using System.ComponentModel;                 // Contient INotifyPropertyChanged et PropertyChangedEventArgs pour la liaison de données (Binding) WPF.
using System.Linq;                           // Contient les opérateurs de requête (ex. : Where) pour filtrer les alertes.
using System.Runtime.CompilerServices;       // Contient [CallerMemberName] pour récupérer automatiquement le nom de la propriété modifiée.
using System.Windows.Input;                  // Contient ICommand et CommandManager pour gérer les commandes des boutons WPF.
using Microsoft.Win32;                       // Contient OpenFileDialog pour ouvrir la boîte de sélection de fichier Windows.
using ScamShield.UI.Models;                  // Donne accès aux modèles de données du projet (ScanResult, ThreatAlert).
using ScamShield.UI.Services;                // Donne accès au contrat IThreatAnalyzerService et à SimulatedThreatAnalyzerService.

namespace ScamShield.UI.ViewModels           // Déclare l'espace de noms (dossier logique) où se trouve ce ViewModel.
{
    // public = accessible depuis d'autres classes du projet ; class = plan de construction d'un objet ;
    // MainViewModel = nom du ViewModel principal ; : INotifyPropertyChanged = contrat qui oblige à prévenir l'UI quand une propriété change.
    public class MainViewModel : INotifyPropertyChanged
    {
        // ====================================================================
        // MODULE 2 : CHAMPS PRIVÉS (DONNÉES INTERNES)
        // Rôle : Garder en mémoire l'état courant de l'application.
        // ====================================================================

        // private = visible uniquement à l'intérieur de MainViewModel ; string = texte ; _targetPath = nom du champ ; = string.Empty = chaîne vide au départ.
        private string _targetPath = string.Empty;

        // Texte affiché pour l'état global de protection au démarrage.
        private string _protectionStatus = "Protégé (simulation locale)";

        // Message d'information affiché à l'utilisateur en bas/statut.
        private string _statusMessage = "Prêt pour une vérification locale.";

        // int = nombre entier (vaut 0 par défaut en C#) pour le dernier score de risque (0 à 100).
        private int _lastRiskScore;

        // Nombre total d'alertes actuellement présentes dans l'historique.
        private int _alertCount;

        // Filtre actif sélectionné pour l'affichage de l'historique ("Tous", "Dangereux", "Suspect", "Sain").
        private string _selectedRiskFilter = "Tous";

        // Dernière zone de risque détectée ("Sain", "Suspect", "Dangereux").
        private string _lastRiskZone = "Sain";

        // Dernier résumé textuel affiché à l'utilisateur.
        private string _lastSummary = "Aucune analyse effectuée pour le moment.";

        // ScanResult? = peut contenir un objet ScanResult ou null (? signifie nullable) tant qu'aucune analyse n'a été faite.
        private ScanResult? _currentResult;

        // readonly = la référence de la collection ne sera pas remplacée par une autre ; new() = crée une nouvelle ObservableCollection vide.
        private readonly ObservableCollection<ThreatAlert> _alertHistory = new();

        // Collection filtrée exposée à la ListView selon SelectedRiskFilter.
        private readonly ObservableCollection<ThreatAlert> _filteredAlertHistory = new();

        // Service injecté qui réalise l'analyse (simulation locale aujourd'hui, Core ou Agent plus tard).
        private readonly IThreatAnalyzerService _analyzerService;

        // ====================================================================
        // MODULE 3 : ÉVÉNEMENT ET PROPRIÉTÉS PUBLIQUES (LIAISON AVEC WPF)
        // Rôle : Permettre au XAML de lire les valeurs et d'être mis à jour.
        // ====================================================================

        // event = événement auquel WPF s'abonne ; PropertyChangedEventHandler? = délégué pouvant être null ; PropertyChanged = nom standard attendu par WPF.
        public event PropertyChangedEventHandler? PropertyChanged;

        // Propriété liée au champ de saisie de la cible dans MainWindow.xaml.
        public string TargetPath
        {
            // get => _targetPath; : quand quelqu'un lit TargetPath, retourne la valeur de _targetPath.
            get => _targetPath;
            // set : quand quelqu'un modifie TargetPath (ex. : l'utilisateur tape du texte).
            set
            {
                // Vérifie si la nouvelle valeur (value) est différente de l'ancienne (_targetPath).
                if (_targetPath != value)
                {
                    // Met à jour le champ privé avec la nouvelle valeur.
                    _targetPath = value;
                    // Notifie l'interface que "TargetPath" a changé.
                    OnPropertyChanged();
                    // Demande à WPF de réévaluer si les boutons (comme "Analyser la cible") peuvent être cliqués.
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        // Propriété publique pour le statut de protection (private set = modifiable seulement depuis cette classe).
        public string ProtectionStatus
        {
            get => _protectionStatus;
            private set
            {
                if (_protectionStatus != value)
                {
                    _protectionStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        // Propriété publique pour le message de statut.
        public string StatusMessage
        {
            get => _statusMessage;
            private set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        // Propriété publique pour le score numérique de risque.
        public int LastRiskScore
        {
            get => _lastRiskScore;
            private set
            {
                if (_lastRiskScore != value)
                {
                    _lastRiskScore = value;
                    OnPropertyChanged();
                }
            }
        }

        // Propriété publique liée au compteur d'alertes dans l'interface WPF.
        public int AlertCount
        {
            get => _alertCount;
            private set
            {
                if (_alertCount != value)
                {
                    _alertCount = value;
                    OnPropertyChanged();
                }
            }
        }

        // Options disponibles dans la ComboBox de filtrage de l'historique.
        public string[] RiskFilterOptions { get; } = ["Tous", "Dangereux", "Suspect", "Sain"];

        // Zone de risque sélectionnée dans la ComboBox de filtrage.
        public string SelectedRiskFilter
        {
            get => _selectedRiskFilter;
            set
            {
                if (_selectedRiskFilter != value)
                {
                    _selectedRiskFilter = value;
                    OnPropertyChanged();
                    RefreshFilteredAlertHistory();
                }
            }
        }

        // Propriété publique pour la zone de risque ("Sain", "Suspect", "Dangereux").
        public string LastRiskZone
        {
            get => _lastRiskZone;
            private set
            {
                if (_lastRiskZone != value)
                {
                    _lastRiskZone = value;
                    OnPropertyChanged();
                }
            }
        }

        // Propriété publique pour le résumé de la dernière analyse.
        public string LastSummary
        {
            get => _lastSummary;
            private set
            {
                if (_lastSummary != value)
                {
                    _lastSummary = value;
                    OnPropertyChanged();
                }
            }
        }

        // Propriété publique contenant l'objet ScanResult courant.
        public ScanResult? CurrentResult
        {
            get => _currentResult;
            private set
            {
                if (_currentResult != value)
                {
                    _currentResult = value;
                    OnPropertyChanged();
                }
            }
        }

        // Expose l'historique complet observable.
        public ObservableCollection<ThreatAlert> AlertHistory => _alertHistory;

        // Expose l'historique filtré observable à la ListView en XAML.
        public ObservableCollection<ThreatAlert> FilteredAlertHistory => _filteredAlertHistory;

        // Commandes publiques reliées aux boutons de la fenêtre ({Binding BrowseFileCommand}, {Binding AnalyzeCommand}, {Binding ResetCommand} et {Binding ClearHistoryCommand}).
        public ICommand BrowseFileCommand { get; }
        public ICommand AnalyzeCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand ClearHistoryCommand { get; }

        // ====================================================================
        // MODULE 4 : CONSTRUCTEURS
        // Rôle : Initialiser l'objet MainViewModel et brancher ses dépendances.
        // ====================================================================

        // Constructeur sans paramètre : appelle l'autre constructeur en fournissant SimulatedThreatAnalyzerService par défaut.
        public MainViewModel() : this(new SimulatedThreatAnalyzerService())
        {
        }

        // Constructeur principal : reçoit n'importe quelle implémentation de IThreatAnalyzerService (injection de dépendance).
        public MainViewModel(IThreatAnalyzerService analyzerService)
        {
            // ?? throw ... : si analyzerService est null, lève une erreur explicite au lieu de planter plus tard.
            _analyzerService = analyzerService ?? throw new ArgumentNullException(nameof(analyzerService));

            // Crée la commande de sélection de fichier via la boîte de dialogue Windows.
            BrowseFileCommand = new SimpleRelayCommand(
                execute: _ => ExecuteBrowseFile(),
                canExecute: _ => true);

            // Crée la commande d'analyse : execute lance ExecuteAnalyze() ; canExecute autorise le clic seulement si TargetPath n'est pas vide.
            AnalyzeCommand = new SimpleRelayCommand(
                execute: _ => ExecuteAnalyze(),
                canExecute: _ => !string.IsNullOrWhiteSpace(TargetPath));

            // Crée la commande de réinitialisation : execute lance ExecuteReset() ; canExecute retourne toujours true.
            ResetCommand = new SimpleRelayCommand(
                execute: _ => ExecuteReset(),
                canExecute: _ => true);

            // Crée la commande d'effacement de l'historique : active seulement si au moins une alerte est présente.
            ClearHistoryCommand = new SimpleRelayCommand(
                execute: _ => ExecuteClearHistory(),
                canExecute: _ => _alertHistory.Count > 0);

            // Initialise la collection filtrée au démarrage.
            RefreshFilteredAlertHistory();
        }

        // ====================================================================
        // MODULE 5 : MÉTHODES MÉTIER DU VIEWMODEL
        // Rôle : Orchestrer l'analyse via le service, la sélection de fichier,
        //        la remise à zéro, le filtrage et le nettoyage de l'historique.
        // ====================================================================

        // Synchronise AlertCount avec le nombre réel d'éléments dans _alertHistory.
        private void UpdateAlertCount()
        {
            AlertCount = _alertHistory.Count;
            CommandManager.InvalidateRequerySuggested();
        }

        // Reconstruit FilteredAlertHistory selon SelectedRiskFilter.
        private void RefreshFilteredAlertHistory()
        {
            _filteredAlertHistory.Clear();

            var matchingAlerts = string.Equals(SelectedRiskFilter, "Tous", StringComparison.OrdinalIgnoreCase)
                ? _alertHistory
                : _alertHistory.Where(a => string.Equals(a.RiskZone, SelectedRiskFilter, StringComparison.OrdinalIgnoreCase));

            foreach (var item in matchingAlerts)
            {
                _filteredAlertHistory.Add(item);
            }
        }

        // Ouvre une boîte de dialogue Windows pour sélectionner un fichier et mettre à jour TargetPath.
        private void ExecuteBrowseFile()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Sélectionner un fichier à analyser",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                TargetPath = dialog.FileName;
                StatusMessage = $"Fichier sélectionné : {dialog.FileName}";
            }
        }

        // Lance l'analyse de la cible saisie par l'utilisateur.
        private void ExecuteAnalyze()
        {
            // try / catch protège l'application contre une exception inattendue pendant l'analyse.
            try
            {
                // .Trim() retire les espaces inutiles au début et à la fin de la chaîne.
                string cleanedTarget = TargetPath.Trim();

                // Vérifie si la cible est vide ou ne contient que des espaces.
                if (string.IsNullOrWhiteSpace(cleanedTarget))
                {
                    StatusMessage = "Veuillez saisir un nom ou chemin de fichier.";
                    return; // Quitte immédiatement la méthode.
                }

                // Appelle le service d'analyse (découplé de la logique de simulation dans le ViewModel).
                ScanResult result = _analyzerService.Analyze(cleanedTarget);

                // Construit un objet ThreatAlert à partir du résultat d'analyse.
                var alert = new ThreatAlert(cleanedTarget, result.RiskScore, result.Summary);

                // Insère la nouvelle alerte à l'index 0 (en haut de la liste) pour voir la plus récente en premier.
                _alertHistory.Insert(0, alert);

                // Met à jour la liste filtrée et le compteur d'alertes après insertion.
                RefreshFilteredAlertHistory();
                UpdateAlertCount();

                // Met à jour le résultat courant et les propriétés affichées à l'écran.
                CurrentResult = result;
                LastRiskScore = alert.RiskScorePercent;
                LastRiskZone = alert.RiskZone;
                LastSummary = $"{alert.TargetName} - {alert.RiskZone} ({alert.RiskScorePercent}/100) : {alert.Reason}";

                // Opérateur ternaire (condition ? valeur_si_vrai : valeur_si_faux) pour adapter le statut global.
                //ProtectionStatus = alert.RiskZone == "Dangereux"
                //    ? "À risque (alerte locale active)"
                //    : "Protégé (simulation locale)";

                // Met à jour l'état global de protection selon la zone de risque détectée.
                ProtectionStatus = alert.RiskZone switch
                {
                    "Dangereux" => "À risque (menace détectée)",
                    "Suspect" => "Vigilance (élément suspect)",
                    _ => "Protégé (simulation locale)"
                };

                // Met à jour le message d'état avec le nombre total d'alertes en mémoire.
                StatusMessage = $"Analyse terminée ({AlertCount} alerte(s) en mémoire).";
            }
            catch (Exception ex)
            {
                // En cas d'erreur, affiche le message de l'exception sans faire planter la fenêtre.
                StatusMessage = $"Erreur lors de l'analyse : {ex.Message}";
            }
        }

        // Réinitialise les champs de la session courante (sans effacer l'historique).
        private void ExecuteReset()
        {
            TargetPath = string.Empty;
            CurrentResult = null;
            LastRiskScore = 0;
            LastRiskZone = "Sain";
            LastSummary = "Aucune analyse effectuée pour le moment.";
            ProtectionStatus = "Protégé (simulation locale)";
            UpdateAlertCount();
            StatusMessage = "Champs réinitialisés.";
        }

        // Efface l'historique des alertes, rafraîchit le filtre et remet le compteur à zéro.
        private void ExecuteClearHistory()
        {
            _alertHistory.Clear();
            RefreshFilteredAlertHistory();
            UpdateAlertCount();
            StatusMessage = "Historique des alertes effacé.";
        }

        // ====================================================================
        // MODULE 6 : NOTIFICATION WPF ET COMMANDE INTERNE
        // Rôle : Déclencher PropertyChanged et implémenter ICommand.
        // ====================================================================

        // [CallerMemberName] remplit automatiquement propertyName avec le nom de la propriété qui appelle cette méthode.
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            // ?. = appel sécurisé si PropertyChanged n'a aucun abonné ; invoke déclenche l'événement.
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // private sealed class = classe privée qu'on ne peut pas hériter, utilisée uniquement ici pour encapsuler ICommand.
        private sealed class SimpleRelayCommand : ICommand
        {
            // Action<object?> = délégué représentant une méthode sans retour prenant un paramètre optionnel.
            private readonly Action<object?> _execute;

            // Predicate<object?>? = délégué retournant un booléen (true/false) pour savoir si la commande est active.
            private readonly Predicate<object?>? _canExecute;

            // Constructeur de SimpleRelayCommand.
            public SimpleRelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            // Relie l'événement CanExecuteChanged au CommandManager de WPF.
            public event EventHandler? CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }

            // Détermine si le bouton associé peut être cliqué.
            public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

            // Exécute l'action associée lorsque l'utilisateur clique.
            public void Execute(object? parameter) => _execute(parameter);
        }
    }
}