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
    /// Représente un élément de quarantaine formaté pour l'affichage WPF.
    /// Pourquoi : Expose les informations essentielles et le pourcentage de risque sans jargon technique.
    /// </summary>
    public sealed class QuarantineDisplayItem
    {
        /// <summary>
        /// Initialise un élément d'affichage à partir d'une entrée fournie par IAgentClient.
        /// Pourquoi : Prépare les libellés lisibles pour la restauration ou la suppression définitive.
        /// </summary>
        public QuarantineDisplayItem(AgentQuarantineEntry entry)
        {
            QuarantineId = entry.QuarantineId;
            FileName = entry.FileName;
            OriginalPath = entry.OriginalPath;
            RiskScore = entry.RiskScore;
            RiskScorePercent = (int)Math.Round(entry.RiskScore * 100.0);
            SummaryReason = string.IsNullOrWhiteSpace(entry.SummaryReason)
                ? "Fichier isolé pour protéger votre système."
                : entry.SummaryReason;
            QuarantinedAtText = entry.QuarantinedAt.ToString("dd/MM HH:mm");
        }

        public string QuarantineId { get; }
        public string FileName { get; }
        public string OriginalPath { get; }
        public double RiskScore { get; }
        public int RiskScorePercent { get; }
        public string SummaryReason { get; }
        public string QuarantinedAtText { get; }
    }

    /// <summary>
    /// ViewModel dédié à l'écran de quarantaine (restaurer ou supprimer un fichier).
    /// Pourquoi : Centralise le chargement via IAgentClient et l'exécution des actions de quarantaine.
    /// </summary>
    public class QuarantineViewModel : INotifyPropertyChanged
    {
        private readonly IAgentClient _agentClient;

        private QuarantineDisplayItem? _selectedItem;
        private bool _isBusy;
        private string _statusMessage = "Chargement de la quarantaine...";

        /// <summary>
        /// Initialise QuarantineViewModel avec un IAgentClient (ou MockAgentClient par défaut).
        /// Pourquoi : Permet de tester la quarantaine avec des données simulées puis avec le named pipe de Jean.
        /// </summary>
        public QuarantineViewModel(IAgentClient? agentClient = null)
        {
            _agentClient = agentClient ?? new MockAgentClient();
            Items = new ObservableCollection<QuarantineDisplayItem>();

            RefreshCommand = new QuarantineRelayCommand(async _ => await LoadQuarantineAsync(), _ => !IsBusy);
            RestoreCommand = new QuarantineRelayCommand(
                async param => await RestoreItemAsync(param as QuarantineDisplayItem ?? SelectedItem),
                param => !IsBusy && (param is QuarantineDisplayItem || SelectedItem is not null));
            DeleteCommand = new QuarantineRelayCommand(
                async param => await DeleteItemAsync(param as QuarantineDisplayItem ?? SelectedItem),
                param => !IsBusy && (param is QuarantineDisplayItem || SelectedItem is not null));

            _agentClient.ThreatAlertReceived += OnThreatAlertReceived;
            _ = LoadQuarantineAsync();
        }

        /// <summary>
        /// Événement de notification WPF lorsqu'une propriété change.
        /// Pourquoi : Synchronise l'état des boutons, compteurs et messages via Binding.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Liste observable des fichiers actuellement en quarantaine.
        /// Pourquoi : Alimente dynamiquement la liste de l'écran de quarantaine.
        /// </summary>
        public ObservableCollection<QuarantineDisplayItem> Items { get; }

        /// <summary>
        /// Élément actuellement sélectionné dans la quarantaine.
        /// Pourquoi : Cible les actions Restaurer ou Supprimer lorsqu'aucun élément précis n'est passé en paramètre.
        /// </summary>
        public QuarantineDisplayItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>
        /// Indique si une opération de chargement, restauration ou suppression est en cours.
        /// Pourquoi : Évite les doubles clics et met à jour la disponibilité des commandes.
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
        /// Message concis décrivant l'état de la quarantaine ou le résultat de la dernière action.
        /// Pourquoi : Donne un retour clair et rassurant à l'utilisateur après chaque action.
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Commande pour recharger la liste des fichiers en quarantaine.
        /// Pourquoi : Permet de synchroniser l'écran avec l'Agent à tout moment.
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// Commande pour restaurer un fichier isolé vers son emplacement d'origine.
        /// Pourquoi : Permet de récupérer un fichier en cas de faux positif.
        /// </summary>
        public ICommand RestoreCommand { get; }

        /// <summary>
        /// Commande pour supprimer définitivement un fichier isolé en quarantaine.
        /// Pourquoi : Permet d'éliminer une menace confirmée.
        /// </summary>
        public ICommand DeleteCommand { get; }

        /// <summary>
        /// Charge les éléments de quarantaine depuis IAgentClient et met à jour la sélection.
        /// Pourquoi : Garde la liste à jour avec l'état réel ou simulé de la quarantaine.
        /// </summary>
        public async Task LoadQuarantineAsync()
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            StatusMessage = "Actualisation des fichiers en quarantaine...";

            try
            {
                IReadOnlyList<AgentQuarantineEntry> entries = await _agentClient.GetQuarantineItemsAsync();

                Items.Clear();
                foreach (AgentQuarantineEntry entry in entries.OrderByDescending(e => e.QuarantinedAt))
                {
                    Items.Add(new QuarantineDisplayItem(entry));
                }

                if (SelectedItem is not null)
                {
                    SelectedItem = Items.FirstOrDefault(i => i.QuarantineId == SelectedItem.QuarantineId) ?? Items.FirstOrDefault();
                }
                else
                {
                    SelectedItem = Items.FirstOrDefault();
                }

                StatusMessage = Items.Count == 0
                    ? "Aucun fichier en quarantaine."
                    : $"{Items.Count} fichier(s) isolé(s) en quarantaine.";
            }
            catch (Exception)
            {
                StatusMessage = "Impossible de charger la quarantaine pour le moment.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Restaure un fichier en quarantaine et recharge la liste.
        /// Pourquoi : Applique la restauration via IAgentClient puis informe l'utilisateur.
        /// </summary>
        public async Task RestoreItemAsync(QuarantineDisplayItem? item)
        {
            if (item is null || IsBusy)
            {
                return;
            }

            IsBusy = true;
            StatusMessage = $"Restauration de {item.FileName}...";

            try
            {
                bool restored = await _agentClient.RestoreQuarantineItemAsync(item.QuarantineId);
                if (!restored)
                {
                    StatusMessage = $"Impossible de restaurer {item.FileName}.";
                    return;
                }

                Items.Remove(item);
                if (Equals(SelectedItem, item))
                {
                    SelectedItem = Items.FirstOrDefault();
                }

                StatusMessage = $"{item.FileName} a été restauré vers son emplacement d'origine.";
            }
            catch (Exception)
            {
                StatusMessage = $"Erreur lors de la restauration de {item.FileName}.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Supprime définitivement un fichier en quarantaine et met à jour la liste.
        /// Pourquoi : Exécute la suppression via IAgentClient et confirme l'action à l'utilisateur.
        /// </summary>
        public async Task DeleteItemAsync(QuarantineDisplayItem? item)
        {
            if (item is null || IsBusy)
            {
                return;
            }

            IsBusy = true;
            StatusMessage = $"Suppression définitive de {item.FileName}...";

            try
            {
                bool deleted = await _agentClient.DeleteQuarantineItemAsync(item.QuarantineId);
                if (!deleted)
                {
                    StatusMessage = $"Impossible de supprimer {item.FileName}.";
                    return;
                }

                Items.Remove(item);
                if (Equals(SelectedItem, item))
                {
                    SelectedItem = Items.FirstOrDefault();
                }

                StatusMessage = $"{item.FileName} a été supprimé définitivement.";
            }
            catch (Exception)
            {
                StatusMessage = $"Erreur lors de la suppression de {item.FileName}.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Recharge automatiquement la quarantaine lorsqu'une nouvelle alerte est reçue.
        /// Pourquoi : Fait apparaître immédiatement un fichier nouvellement mis en quarantaine.
        /// </summary>
        private async void OnThreatAlertReceived(object? sender, AgentAlertReport report)
        {
            if (report.IsQuarantined)
            {
                await LoadQuarantineAsync();
            }
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
        /// Implémentation légère de ICommand pour les actions asynchrones de quarantaine.
        /// Pourquoi : Permet de lier RefreshCommand, RestoreCommand et DeleteCommand sans dépendance externe.
        /// </summary>
        private sealed class QuarantineRelayCommand : ICommand
        {
            private readonly Func<object?, Task> _execute;
            private readonly Predicate<object?>? _canExecute;

            public QuarantineRelayCommand(Func<object?, Task> execute, Predicate<object?>? canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
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
                await _execute(parameter);
            }
        }
    }
}