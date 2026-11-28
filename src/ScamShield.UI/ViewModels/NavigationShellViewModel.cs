using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using ScamShield.UI.Services;

namespace ScamShield.UI.ViewModels
{
    /// <summary>
    /// Sections disponibles dans la navigation principale de ScamShield.UI.
    /// Pourquoi : Permet de basculer clairement entre le tableau de bord, les alertes par zone,
    /// la quarantaine, les paramètres et l'assistant de dépannage sans modifier MainViewModel.
    /// </summary>
    public enum ShellSection
    {
        TableauDeBord,
        AlertesParZone,
        Quarantaine,
        Parametres,
        AssistantDepannage
    }

    /// <summary>
    /// ViewModel dédié à la navigation multi-vues et à l'état global de protection.
    /// Pourquoi : Garde MainViewModel intact tout en coordonnant les vues de Joseph
    /// et les notifications Windows lorsqu'une menace est bloquée.
    /// </summary>
    public class NavigationShellViewModel : INotifyPropertyChanged
    {
        private readonly IAgentClient _agentClient;
        private readonly WindowsNotificationService _notificationService;

        private ShellSection _currentSection = ShellSection.TableauDeBord;
        private AgentProtectionStatus _protectionStatus = AgentProtectionStatus.Protege;
        private string _protectionStatusLabel = "Protégé";
        private string _protectionSummary = "Votre système est protégé. Les alertes et fichiers isolés sont suivis clairement.";
        private bool _isRiskState;
        private string _lastActivityMessage = "Prêt — surveillance active.";

        /// <summary>
        /// Initialise NavigationShellViewModel avec un IAgentClient (ou MockAgentClient par défaut).
        /// Pourquoi : Permet de faire fonctionner les vues dès maintenant et de brancher
        /// le service named pipe de Jean plus tard sans casser l'existant.
        /// </summary>
        public NavigationShellViewModel(IAgentClient? agentClient = null)
        {
            _agentClient = agentClient ?? new MockAgentClient();
            _notificationService = new WindowsNotificationService(_agentClient);

            DashboardViewModel = new DashboardViewModel();
            AlertsViewModel = new AlertsViewModel(_agentClient);
            QuarantineViewModel = new QuarantineViewModel(_agentClient);
            SettingsViewModel = new SettingsViewModel(_agentClient);
            TroubleshootingViewModel = new TroubleshootingViewModel();

            NavigateCommand = new ShellRelayCommand(param =>
            {
                if (param is ShellSection section)
                {
                    CurrentSection = section;
                }
            });

            RefreshStatusCommand = new ShellRelayCommand(async _ => await RefreshProtectionStatusAsync());

            _agentClient.ThreatAlertReceived += OnThreatAlertReceived;
            _ = RefreshProtectionStatusAsync();
        }

        /// <summary>
        /// Événement de notification WPF lorsqu'une propriété change.
        /// Pourquoi : Met à jour la vue active et l'indicateur de protection via Binding.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        public DashboardViewModel DashboardViewModel { get; }
        public AlertsViewModel AlertsViewModel { get; }
        public QuarantineViewModel QuarantineViewModel { get; }
        public SettingsViewModel SettingsViewModel { get; }
        public TroubleshootingViewModel TroubleshootingViewModel { get; }

        /// <summary>
        /// Section actuellement sélectionnée dans la navigation multi-vues.
        /// Pourquoi : Détermine quelle vue est affichée dans la fenêtre de navigation.
        /// </summary>
        public ShellSection CurrentSection
        {
            get => _currentSection;
            set => SetProperty(ref _currentSection, value);
        }

        /// <summary>
        /// État global de protection (Protege ou ARisque).
        /// Pourquoi : Reflète si le système est protégé ou exposé à une menace non isolée.
        /// </summary>
        public AgentProtectionStatus ProtectionStatus
        {
            get => _protectionStatus;
            private set
            {
                if (SetProperty(ref _protectionStatus, value))
                {
                    IsRiskState = value == AgentProtectionStatus.ARisque;
                    ProtectionStatusLabel = IsRiskState ? "À risque" : "Protégé";
                    ProtectionSummary = IsRiskState
                        ? "Attention : un fichier dangereux a été détecté. Vérifiez les alertes ou la quarantaine."
                        : "Votre PC est protégé. Aucune menace active non isolée n'est signalée.";
                }
            }
        }

        /// <summary>
        /// Libellé affiché ("Protégé" ou "À risque").
        /// Pourquoi : Donne une réponse immédiate et claire à l'utilisateur.
        /// </summary>
        public string ProtectionStatusLabel
        {
            get => _protectionStatusLabel;
            private set => SetProperty(ref _protectionStatusLabel, value);
        }

        /// <summary>
        /// Explication courte de l'état global sans jargon technique.
        /// Pourquoi : Aide l'utilisateur à comprendre rapidement la situation.
        /// </summary>
        public string ProtectionSummary
        {
            get => _protectionSummary;
            private set => SetProperty(ref _protectionSummary, value);
        }

        /// <summary>
        /// Vrai lorsque l'état global est "À risque".
        /// Pourquoi : Permet d'adapter visuellement l'alerte d'état dans la fenêtre.
        /// </summary>
        public bool IsRiskState
        {
            get => _isRiskState;
            private set => SetProperty(ref _isRiskState, value);
        }

        /// <summary>
        /// Message concis décrivant la dernière vérification ou alerte reçue.
        /// Pourquoi : Garde un suivi clair de l'activité récente.
        /// </summary>
        public string LastActivityMessage
        {
            get => _lastActivityMessage;
            private set => SetProperty(ref _lastActivityMessage, value);
        }

        /// <summary>
        /// Commande permettant de changer de section dans la navigation multi-vues.
        /// Pourquoi : Relie les boutons de navigation de la fenêtre.
        /// </summary>
        public ICommand NavigateCommand { get; }

        /// <summary>
        /// Commande permettant de rafraîchir l'état global et les données des sous-vues.
        /// Pourquoi : Synchronise l'état de protection, les alertes et la quarantaine.
        /// </summary>
        public ICommand RefreshStatusCommand { get; }

        /// <summary>
        /// Met à jour l'état de protection (Protégé / À risque) via IAgentClient.
        /// Pourquoi : Maintient l'indicateur principal aligné avec le service.
        /// </summary>
        public async Task RefreshProtectionStatusAsync()
        {
            try
            {
                ProtectionStatus = await _agentClient.GetProtectionStatusAsync();
                LastActivityMessage = $"État vérifié ({ProtectionStatusLabel}) • {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception)
            {
                LastActivityMessage = "Impossible de vérifier l'état de protection pour le moment.";
            }
        }

        /// <summary>
        /// Réagit à l'arrivée d'une alerte : déclenche la notification Windows si la menace est dangereuse
        /// et actualise l'état global, les alertes et la quarantaine.
        /// Pourquoi : Assure un comportement cohérent de bout en bout.
        /// </summary>
        private async void OnThreatAlertReceived(object? sender, AgentAlertReport alert)
        {
            if (alert.Zone == AgentThreatZone.Dangereux)
            {
                _notificationService.NotifyThreatBlocked(alert);
                LastActivityMessage = $"Menace bloquée : {alert.FileName} ({DateTime.Now:HH:mm:ss})";
            }
            else if (alert.Zone == AgentThreatZone.Suspect)
            {
                LastActivityMessage = $"Fichier suspect : {alert.FileName} ({DateTime.Now:HH:mm:ss})";
            }
            else
            {
                LastActivityMessage = $"Fichier sain : {alert.FileName} ({DateTime.Now:HH:mm:ss})";
            }

            await RefreshProtectionStatusAsync();
            await AlertsViewModel.LoadAlertsAsync();
            await QuarantineViewModel.LoadQuarantineAsync();
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
        /// Implémentation légère de ICommand pour la navigation et le rafraîchissement.
        /// Pourquoi : Permet le Binding WPF sans dépendance externe.
        /// </summary>
        private sealed class ShellRelayCommand : ICommand
        {
            private readonly Func<object?, Task>? _asyncExecute;
            private readonly Action<object?>? _syncExecute;

            public ShellRelayCommand(Func<object?, Task> asyncExecute)
            {
                _asyncExecute = asyncExecute ?? throw new ArgumentNullException(nameof(asyncExecute));
            }

            public ShellRelayCommand(Action<object?> syncExecute)
            {
                _syncExecute = syncExecute ?? throw new ArgumentNullException(nameof(syncExecute));
            }

            public event EventHandler? CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }

            public bool CanExecute(object? parameter) => true;

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