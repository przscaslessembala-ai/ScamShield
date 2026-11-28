using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using ScamShield.UI.Services;

namespace ScamShield.UI.ViewModels
{
    /// <summary>
    /// ViewModel dédié à l'écran des paramètres de ScamShield.UI.
    /// Pourquoi : Centralise la gestion des dossiers surveillés, du niveau de sensibilité
    /// et de la liste blanche via le contrat IAgentClient.
    /// </summary>
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly IAgentClient _agentClient;

        private string _selectedSensitivity = "Équilibrée";
        private string _newWatchedFolder = string.Empty;
        private string _newAllowListEntry = string.Empty;
        private bool _isBusy;
        private string _statusMessage = "Chargement des paramètres...";

        /// <summary>
        /// Initialise SettingsViewModel avec un IAgentClient (ou MockAgentClient par défaut).
        /// Pourquoi : Permet de tester la configuration avec des données fictives dès maintenant
        /// puis de la synchroniser avec le service Agent via named pipe.
        /// </summary>
        public SettingsViewModel(IAgentClient? agentClient = null)
        {
            _agentClient = agentClient ?? new MockAgentClient();

            SensitivityLevels = new ObservableCollection<string>
            {
                "Faible",
                "Équilibrée",
                "Élevée"
            };

            WatchedFolders = new ObservableCollection<string>();
            AllowList = new ObservableCollection<string>();

            AddWatchedFolderCommand = new SettingsRelayCommand(_ => AddWatchedFolder(), _ => !IsBusy);
            RemoveWatchedFolderCommand = new SettingsRelayCommand(
                param => RemoveWatchedFolder(param as string),
                param => !IsBusy && param is string);

            AddAllowListCommand = new SettingsRelayCommand(_ => AddAllowListEntry(), _ => !IsBusy);
            RemoveAllowListCommand = new SettingsRelayCommand(
                param => RemoveAllowListEntry(param as string),
                param => !IsBusy && param is string);

            SaveSettingsCommand = new SettingsRelayCommand(async _ => await SaveSettingsAsync(), _ => !IsBusy);
            ReloadSettingsCommand = new SettingsRelayCommand(async _ => await LoadSettingsAsync(), _ => !IsBusy);

            _ = LoadSettingsAsync();
        }

        /// <summary>
        /// Événement de notification WPF lorsqu'une propriété change.
        /// Pourquoi : Met à jour les contrôles liés par Binding.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Niveaux de sensibilité disponibles pour l'analyse des fichiers.
        /// Pourquoi : Offre des choix clairs et compréhensibles sans jargon technique.
        /// </summary>
        public ObservableCollection<string> SensitivityLevels { get; }

        /// <summary>
        /// Liste observable des dossiers surveillés en temps réel.
        /// Pourquoi : Permet d'ajouter ou retirer des dossiers surveillés dans l'interface.
        /// </summary>
        public ObservableCollection<string> WatchedFolders { get; }

        /// <summary>
        /// Liste observable des chemins ou fichiers autorisés (liste blanche).
        /// Pourquoi : Permet d'exclure des outils ou fichiers de confiance de manière explicite.
        /// </summary>
        public ObservableCollection<string> AllowList { get; }

        /// <summary>
        /// Niveau de sensibilité actuellement sélectionné.
        /// Pourquoi : Détermine la rigueur de détection appliquée par le système.
        /// </summary>
        public string SelectedSensitivity
        {
            get => _selectedSensitivity;
            set => SetProperty(ref _selectedSensitivity, value);
        }

        /// <summary>
        /// Champ de saisie pour ajouter un nouveau dossier surveillé.
        /// Pourquoi : Lie la zone texte d'ajout de dossier au ViewModel.
        /// </summary>
        public string NewWatchedFolder
        {
            get => _newWatchedFolder;
            set => SetProperty(ref _newWatchedFolder, value);
        }

        /// <summary>
        /// Champ de saisie pour ajouter un élément à la liste blanche.
        /// Pourquoi : Lie la zone texte d'ajout de liste blanche au ViewModel.
        /// </summary>
        public string NewAllowListEntry
        {
            get => _newAllowListEntry;
            set => SetProperty(ref _newAllowListEntry, value);
        }

        /// <summary>
        /// Indique si un chargement ou un enregistrement est en cours.
        /// Pourquoi : Désactive temporairement les actions pendant une opération asynchrone.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>
        /// Message concis décrivant l'état des paramètres ou la confirmation d'enregistrement.
        /// Pourquoi : Informe l'utilisateur sobrement après chaque action.
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Commande pour ajouter un dossier à la liste des dossiers surveillés.
        /// Pourquoi : Valide et insère le chemin saisi dans WatchedFolders.
        /// </summary>
        public ICommand AddWatchedFolderCommand { get; }

        /// <summary>
        /// Commande pour retirer un dossier de WatchedFolders.
        /// Pourquoi : Permet à l'utilisateur de cesser de surveiller un dossier.
        /// </summary>
        public ICommand RemoveWatchedFolderCommand { get; }

        /// <summary>
        /// Commande pour ajouter un élément à la liste blanche.
        /// Pourquoi : Valide et insère le chemin saisi dans AllowList.
        /// </summary>
        public ICommand AddAllowListCommand { get; }

        /// <summary>
        /// Commande pour retirer un élément de la liste blanche.
        /// Pourquoi : Permet de réactiver la surveillance sur un élément précédemment exclu.
        /// </summary>
        public ICommand RemoveAllowListCommand { get; }

        /// <summary>
        /// Commande pour enregistrer la configuration complète auprès de IAgentClient.
        /// Pourquoi : Persiste les dossiers surveillés, la sensibilité et la liste blanche.
        /// </summary>
        public ICommand SaveSettingsCommand { get; }

        /// <summary>
        /// Commande pour recharger la configuration enregistrée.
        /// Pourquoi : Permet d'annuler des modifications non enregistrées.
        /// </summary>
        public ICommand ReloadSettingsCommand { get; }

        /// <summary>
        /// Charge la configuration depuis IAgentClient et alimente les collections observables.
        /// Pourquoi : Synchronise l'écran des paramètres avec l'état courant du service.
        /// </summary>
        public async Task LoadSettingsAsync()
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            StatusMessage = "Chargement des paramètres...";

            try
            {
                AgentProtectionConfiguration config = await _agentClient.GetConfigurationAsync();

                WatchedFolders.Clear();
                foreach (string folder in config.WatchedFolders)
                {
                    WatchedFolders.Add(folder);
                }

                AllowList.Clear();
                foreach (string entry in config.AllowList)
                {
                    AllowList.Add(entry);
                }

                SelectedSensitivity = SensitivityLevels.Contains(config.SensitivityLevel)
                    ? config.SensitivityLevel
                    : "Équilibrée";

                StatusMessage = "Paramètres chargés.";
            }
            catch (Exception)
            {
                StatusMessage = "Impossible de charger les paramètres pour le moment.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Enregistre les paramètres actuels (dossiers surveillés, sensibilité, liste blanche) via IAgentClient.
        /// Pourquoi : Applique les préférences de l'utilisateur et confirme le résultat.
        /// </summary>
        public async Task SaveSettingsAsync()
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            StatusMessage = "Enregistrement des paramètres...";

            try
            {
                var configuration = new AgentProtectionConfiguration(
                    watchedFolders: WatchedFolders.ToList(),
                    sensitivityLevel: SelectedSensitivity,
                    allowList: AllowList.ToList());

                bool saved = await _agentClient.SaveConfigurationAsync(configuration);
                StatusMessage = saved
                    ? $"Paramètres enregistrés ({SelectedSensitivity}, {WatchedFolders.Count} dossier(s) surveillé(s))."
                    : "L'enregistrement des paramètres a échoué.";
            }
            catch (Exception)
            {
                StatusMessage = "Erreur lors de l'enregistrement des paramètres.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Ajoute un dossier saisi à WatchedFolders s'il est valide et absent de la liste.
        /// Pourquoi : Évite les entrées vides ou en double.
        /// </summary>
        private void AddWatchedFolder()
        {
            string candidate = NewWatchedFolder.Trim();
            if (string.IsNullOrWhiteSpace(candidate))
            {
                StatusMessage = "Indiquez un chemin de dossier avant d'ajouter.";
                return;
            }

            if (WatchedFolders.Any(f => string.Equals(f, candidate, StringComparison.OrdinalIgnoreCase)))
            {
                StatusMessage = "Ce dossier figure déjà dans la liste surveillée.";
                return;
            }

            WatchedFolders.Add(candidate);
            NewWatchedFolder = string.Empty;
            StatusMessage = $"Dossier ajouté : {candidate} (pensez à enregistrer).";
        }

        /// <summary>
        /// Supprime un dossier de WatchedFolders.
        /// Pourquoi : Permet de retirer un dossier de la surveillance.
        /// </summary>
        private void RemoveWatchedFolder(string? folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                return;
            }

            WatchedFolders.Remove(folder);
            StatusMessage = $"Dossier retiré : {folder} (pensez à enregistrer).";
        }

        /// <summary>
        /// Ajoute un élément saisi à AllowList s'il est valide et absent de la liste.
        /// Pourquoi : Évite les doublons dans la liste blanche.
        /// </summary>
        private void AddAllowListEntry()
        {
            string candidate = NewAllowListEntry.Trim();
            if (string.IsNullOrWhiteSpace(candidate))
            {
                StatusMessage = "Indiquez un chemin de fichier ou de dossier pour la liste blanche.";
                return;
            }

            if (AllowList.Any(a => string.Equals(a, candidate, StringComparison.OrdinalIgnoreCase)))
            {
                StatusMessage = "Cet élément est déjà présent dans la liste blanche.";
                return;
            }

            AllowList.Add(candidate);
            NewAllowListEntry = string.Empty;
            StatusMessage = $"Ajouté à la liste blanche : {candidate} (pensez à enregistrer).";
        }

        /// <summary>
        /// Supprime un élément de AllowList.
        /// Pourquoi : Permet de retirer un élément de la liste blanche.
        /// </summary>
        private void RemoveAllowListEntry(string? entry)
        {
            if (string.IsNullOrWhiteSpace(entry))
            {
                return;
            }

            AllowList.Remove(entry);
            StatusMessage = $"Retiré de la liste blanche : {entry} (pensez à enregistrer).";
        }

        /// <summary>
        /// Met à jour un champ et déclenche PropertyChanged si la valeur change.
        /// Pourquoi : Factorise INotifyPropertyChanged proprement.
        /// </summary>
        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
            {
                return false;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        /// <summary>
        /// Implémentation légère de ICommand prenant en charge les actions synchrones et asynchrones.
        /// Pourquoi : Relie les contrôles WPF au ViewModel sans dépendance externe.
        /// </summary>
        private sealed class SettingsRelayCommand : ICommand
        {
            private readonly Func<object?, Task>? _asyncExecute;
            private readonly Action<object?>? _syncExecute;
            private readonly Predicate<object?>? _canExecute;

            public SettingsRelayCommand(Func<object?, Task> asyncExecute, Predicate<object?>? canExecute = null)
            {
                _asyncExecute = asyncExecute ?? throw new ArgumentNullException(nameof(asyncExecute));
                _canExecute = canExecute;
            }

            public SettingsRelayCommand(Action<object?> syncExecute, Predicate<object?>? canExecute = null)
            {
                _syncExecute = syncExecute ?? throw new ArgumentNullException(nameof(syncExecute));
                _canExecute = canExecute;
            }

            public event EventHandler? CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }

            public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

            public async void Execute(object? parameter)
            {
                if (_asyncExecute is not null)
                {
                    await _asyncExecute(parameter);
                }
                else
                {
                    _syncExecute?.Invoke(parameter);
                }
            }
        }
    }
}