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
    /// Filtres disponibles pour parcourir les alertes affichées à l'utilisateur.
    /// Pourquoi : Permet de consulter toutes les alertes ou de cibler une zone précise (Dangereux, Suspect, Sain).
    /// </summary>
    public enum AlertZoneFilter
    {
        Toutes,
        Dangereux,
        Suspect,
        Sain
    }

    /// <summary>
    /// Modèle d'affichage d'une alerte destiné à la vue des alertes.
    /// Pourquoi : Formate le score, la zone et les raisons en français simple sans jargon technique.
    /// </summary>
    public sealed class AlertDisplayItem
    {
        /// <summary>
        /// Initialise un élément d'alerte prêt pour l'affichage WPF.
        /// Pourquoi : Regroupe les informations essentielles pour comprendre en moins de 10 secondes pourquoi un fichier est signalé.
        /// </summary>
        public AlertDisplayItem(AgentAlertReport report)
        {
            AlertId = report.AlertId;
            FileName = report.FileName;
            FilePath = report.FilePath;
            RiskScore = report.RiskScore;
            RiskScorePercent = (int)Math.Round(report.RiskScore * 100.0);
            Zone = report.Zone;
            DetectedAtText = report.DetectedAt.ToString("dd/MM HH:mm");
            IsQuarantined = report.IsQuarantined;
            Reasons = report.Reasons.Count > 0
                ? report.Reasons
                : ["Aucune explication détaillée fournie pour ce fichier."];
        }

        public string AlertId { get; }
        public string FileName { get; }
        public string FilePath { get; }
        public double RiskScore { get; }
        public int RiskScorePercent { get; }
        public AgentThreatZone Zone { get; }
        public string DetectedAtText { get; }
        public bool IsQuarantined { get; }
        public IReadOnlyList<string> Reasons { get; }

        /// <summary>
        /// Libellé clair de la zone (Sain, Suspect, Dangereux).
        /// Pourquoi : Identifie immédiatement le niveau de risque du fichier.
        /// </summary>
        public string ZoneLabel => Zone switch
        {
            AgentThreatZone.Dangereux => "Dangereux",
            AgentThreatZone.Suspect => "Suspect",
            _ => "Sain"
        };

        /// <summary>
        /// Indication simple sur l'action à adopter.
        /// Pourquoi : Aide l'utilisateur à savoir quoi faire sans jargon technique.
        /// </summary>
        public string RecommendedAction => Zone switch
        {
            AgentThreatZone.Dangereux => IsQuarantined
                ? "Fichier isolé en quarantaine. Vous pouvez le vérifier ou le supprimer définitivement."
                : "À bloquer ou isoler immédiatement. Évitez d'ouvrir ce fichier.",
            AgentThreatZone.Suspect => "À vérifier avant ouverture. Assurez-vous de connaître l'origine du fichier.",
            _ => "Aucun problème détecté. Vous pouvez utiliser ce fichier normalement."
        };
    }

    /// <summary>
    /// ViewModel dédié à la liste des alertes réparties par zone (Sain, Suspect, Dangereux).
    /// Pourquoi : Centralise le chargement via IAgentClient, le filtrage par zone et les compteurs.
    /// </summary>
    public class AlertsViewModel : INotifyPropertyChanged
    {
        private readonly IAgentClient _agentClient;
        private readonly List<AlertDisplayItem> _allAlerts = [];

        private AlertZoneFilter _selectedFilter = AlertZoneFilter.Toutes;
        private AlertDisplayItem? _selectedAlert;
        private bool _isLoading;
        private string _statusMessage = "Chargement des alertes...";
        private int _dangerousCount;
        private int _suspectCount;
        private int _safeCount;

        /// <summary>
        /// Initialise AlertsViewModel avec un IAgentClient (ou MockAgentClient par défaut).
        /// Pourquoi : Permet de fonctionner avec des données fictives dès maintenant et de brancher le named pipe plus tard.
        /// </summary>
        public AlertsViewModel(IAgentClient? agentClient = null)
        {
            _agentClient = agentClient ?? new MockAgentClient();

            VisibleAlerts = new ObservableCollection<AlertDisplayItem>();
            DangerousAlerts = new ObservableCollection<AlertDisplayItem>();
            SuspectAlerts = new ObservableCollection<AlertDisplayItem>();
            SafeAlerts = new ObservableCollection<AlertDisplayItem>();

            RefreshAlertsCommand = new AlertsRelayCommand(async _ => await LoadAlertsAsync(), _ => !IsLoading);
            SelectFilterCommand = new AlertsRelayCommand(param =>
            {
                if (param is AlertZoneFilter filter)
                {
                    SelectedFilter = filter;
                }
            });

            _agentClient.ThreatAlertReceived += OnThreatAlertReceived;
            _ = LoadAlertsAsync();
        }

        /// <summary>
        /// Événement de notification WPF lorsqu'une propriété change.
        /// Pourquoi : Met à jour les contrôles liés par Binding.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Filtre actif sélectionné par l'utilisateur.
        /// Pourquoi : Détermine quelles alertes sont affichées dans la liste courante.
        /// </summary>
        public AlertZoneFilter SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                if (SetProperty(ref _selectedFilter, value))
                {
                    ApplyFilter();
                }
            }
        }

        /// <summary>
        /// Alerte actuellement sélectionnée pour consulter le score et les raisons.
        /// Pourquoi : Met en avant l'explication complète du fichier choisi.
        /// </summary>
        public AlertDisplayItem? SelectedAlert
        {
            get => _selectedAlert;
            set => SetProperty(ref _selectedAlert, value);
        }

        /// <summary>
        /// Indique si le chargement des alertes est en cours.
        /// Pourquoi : Empêche les rechargements multiples simultanés.
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Message d'état concis affiché dans la vue des alertes.
        /// Pourquoi : Indique le nombre d'alertes et l'heure de mise à jour.
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Nombre d'alertes dans la zone Dangereux.
        /// Pourquoi : Alimente le badge/compteur de la zone Dangereux.
        /// </summary>
        public int DangerousCount
        {
            get => _dangerousCount;
            private set => SetProperty(ref _dangerousCount, value);
        }

        /// <summary>
        /// Nombre d'alertes dans la zone Suspect.
        /// Pourquoi : Alimente le badge/compteur de la zone Suspect.
        /// </summary>
        public int SuspectCount
        {
            get => _suspectCount;
            private set => SetProperty(ref _suspectCount, value);
        }

        /// <summary>
        /// Nombre d'alertes dans la zone Sain.
        /// Pourquoi : Alimente le badge/compteur de la zone Sain.
        /// </summary>
        public int SafeCount
        {
            get => _safeCount;
            private set => SetProperty(ref _safeCount, value);
        }

        /// <summary>
        /// Liste filtrée des alertes affichées à l'écran.
        /// Pourquoi : Fournit les éléments correspondant au filtre sélectionné.
        /// </summary>
        public ObservableCollection<AlertDisplayItem> VisibleAlerts { get; }

        /// <summary>
        /// Alertes de la zone Dangereux.
        /// Pourquoi : Regroupe les fichiers classés dangereux.
        /// </summary>
        public ObservableCollection<AlertDisplayItem> DangerousAlerts { get; }

        /// <summary>
        /// Alertes de la zone Suspect.
        /// Pourquoi : Regroupe les fichiers classés suspects.
        /// </summary>
        public ObservableCollection<AlertDisplayItem> SuspectAlerts { get; }

        /// <summary>
        /// Alertes de la zone Sain.
        /// Pourquoi : Regroupe les fichiers classés sains.
        /// </summary>
        public ObservableCollection<AlertDisplayItem> SafeAlerts { get; }

        /// <summary>
        /// Commande pour recharger les alertes depuis IAgentClient.
        /// Pourquoi : Permet d'actualiser la liste à la demande.
        /// </summary>
        public ICommand RefreshAlertsCommand { get; }

        /// <summary>
        /// Commande pour sélectionner un filtre de zone.
        /// Pourquoi : Relie les boutons de zone au ViewModel.
        /// </summary>
        public ICommand SelectFilterCommand { get; }

        /// <summary>
        /// Charge les alertes depuis IAgentClient, les classe par zone et applique le filtre actif.
        /// Pourquoi : Maintient les compteurs et la liste filtrée synchronisés.
        /// </summary>
        public async Task LoadAlertsAsync()
        {
            if (IsLoading)
            {
                return;
            }

            IsLoading = true;
            StatusMessage = "Actualisation des alertes...";

            try
            {
                IReadOnlyList<AgentAlertReport> reports = await _agentClient.GetAlertsAsync();

                _allAlerts.Clear();
                DangerousAlerts.Clear();
                SuspectAlerts.Clear();
                SafeAlerts.Clear();

                foreach (AgentAlertReport report in reports.OrderByDescending(r => r.DetectedAt))
                {
                    var item = new AlertDisplayItem(report);
                    _allAlerts.Add(item);

                    switch (item.Zone)
                    {
                        case AgentThreatZone.Dangereux:
                            DangerousAlerts.Add(item);
                            break;
                        case AgentThreatZone.Suspect:
                            SuspectAlerts.Add(item);
                            break;
                        default:
                            SafeAlerts.Add(item);
                            break;
                    }
                }

                DangerousCount = DangerousAlerts.Count;
                SuspectCount = SuspectAlerts.Count;
                SafeCount = SafeAlerts.Count;

                ApplyFilter();
                StatusMessage = $"{_allAlerts.Count} fichier(s) analysé(s) • Mise à jour à {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception)
            {
                StatusMessage = "Impossible de charger les alertes pour le moment.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Réagit à l'arrivée d'une nouvelle alerte envoyée par l'Agent.
        /// Pourquoi : Met à jour la liste et sélectionne la nouvelle alerte.
        /// </summary>
        private async void OnThreatAlertReceived(object? sender, AgentAlertReport report)
        {
            await LoadAlertsAsync();
            AlertDisplayItem? matching = _allAlerts.FirstOrDefault(a => a.AlertId == report.AlertId);
            if (matching is not null)
            {
                SelectedAlert = matching;
            }
        }

        /// <summary>
        /// Filtre les alertes selon SelectedFilter et met à jour VisibleAlerts.
        /// Pourquoi : Permet de passer d'une zone à l'autre de manière fluide.
        /// </summary>
        private void ApplyFilter()
        {
            VisibleAlerts.Clear();

            IEnumerable<AlertDisplayItem> filtered = SelectedFilter switch
            {
                AlertZoneFilter.Dangereux => _allAlerts.Where(a => a.Zone == AgentThreatZone.Dangereux),
                AlertZoneFilter.Suspect => _allAlerts.Where(a => a.Zone == AgentThreatZone.Suspect),
                AlertZoneFilter.Sain => _allAlerts.Where(a => a.Zone == AgentThreatZone.Sain),
                _ => _allAlerts
            };

            foreach (AlertDisplayItem item in filtered)
            {
                VisibleAlerts.Add(item);
            }

            if (SelectedAlert is null || !VisibleAlerts.Contains(SelectedAlert))
            {
                SelectedAlert = VisibleAlerts.FirstOrDefault();
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
        /// Implémentation légère de ICommand pour les actions du ViewModel.
        /// Pourquoi : Permet de lier les boutons WPF sans dépendance externe.
        /// </summary>
        private sealed class AlertsRelayCommand : ICommand
        {
            private readonly Func<object?, Task>? _asyncExecute;
            private readonly Action<object?>? _syncExecute;
            private readonly Predicate<object?>? _canExecute;

            public AlertsRelayCommand(Func<object?, Task> asyncExecute, Predicate<object?>? canExecute = null)
            {
                _asyncExecute = asyncExecute ?? throw new ArgumentNullException(nameof(asyncExecute));
                _canExecute = canExecute;
            }

            public AlertsRelayCommand(Action<object?> syncExecute, Predicate<object?>? canExecute = null)
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