using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;

namespace ScamShield.UI.ViewModels
{
    /// <summary>
    /// Représente l'état global de protection affiché sur la carte principale du tableau de bord.
    /// Pourquoi : Permet de lier facilement l'indicateur visuel (couleur, libellé et animation) à un état typé.
    /// </summary>
    public enum ProtectionLevel
    {
        Protege,
        Attention,
        Menace
    }

    /// <summary>
    /// Représente le niveau de gravité d'un événement récent dans le journal d'activité.
    /// Pourquoi : Évite les chaînes magiques et permet d'associer une couleur de badge cohérente.
    /// </summary>
    public enum EventSeverity
    {
        Info,
        Avertissement,
        Critique
    }

    /// <summary>
    /// Représente un point horaire de la courbe d'activité des 24 dernières heures.
    /// Pourquoi : Regroupe l'heure, le volume d'activité et un résumé lisible pour l'infobulle au survol.
    /// </summary>
    public sealed class HourlyActivityPoint
    {
        /// <summary>
        /// Initialise un point horaire d'activité fictive.
        /// Pourquoi : Garantit que chaque point possède un libellé horaire, une valeur numérique et un détail contextuel.
        /// </summary>
        public HourlyActivityPoint(string hourLabel, double activityValue, string details)
        {
            HourLabel = hourLabel;
            ActivityValue = activityValue;
            Details = details;
        }

        public string HourLabel { get; }
        public double ActivityValue { get; }
        public string Details { get; }
    }

    /// <summary>
    /// Représente une entrée de la liste des événements récents surveillés par l'antivirus IA.
    /// Pourquoi : Fournit à la vue les informations essentielles (heure, titre, contexte et gravité).
    /// </summary>
    public sealed class RecentSecurityEvent
    {
        /// <summary>
        /// Initialise un événement récent fictif.
        /// Pourquoi : Structure de manière immuable les données affichées dans chaque ligne d'événement.
        /// </summary>
        public RecentSecurityEvent(string timestamp, string title, string context, EventSeverity severity)
        {
            Timestamp = timestamp;
            Title = title;
            Context = context;
            Severity = severity;
        }

        public string Timestamp { get; }
        public string Title { get; }
        public string Context { get; }
        public EventSeverity Severity { get; }
    }

    /// <summary>
    /// ViewModel dédié exclusivement au tableau de bord (DashboardView).
    /// Pourquoi : Centralise les données fictives, l'horloge en direct, l'état de l'IA et la simulation d'analyse rapide
    /// tout en notifiant l'interface WPF via INotifyPropertyChanged.
    /// </summary>
    public class DashboardViewModel : INotifyPropertyChanged
    {
        // Minuteur WPF exécuté sur le thread UI pour mettre à jour l'horloge en direct chaque seconde.
        private readonly DispatcherTimer _clockTimer;

        // Minuteur utilisé pour simuler brièvement la fin d'une "Analyse rapide" fictive.
        private readonly DispatcherTimer _scanSimulationTimer;

        // Générateur pseudo-aléatoire utilisé uniquement pour faire varier les données fictives lors d'une analyse rapide.
        private readonly Random _random = new();

        // Champs d'état interne exposés via des propriétés observables.
        private string _appName = "ScamShield AI";
        private string _appTagline = "Antivirus IA • Surveillance système & Récupération d'accès";
        private string _liveClockText = DateTime.Now.ToString("dddd dd MMMM yyyy • HH:mm:ss");
        private ProtectionLevel _protectionState = ProtectionLevel.Protege;
        private string _protectionTitle = "Protégé";
        private string _protectionSubtitle = "Surveillance active en temps réel et module de récupération prêt.";
        private int _filesScannedCount = 148920;
        private int _threatsBlockedCount = 37;
        private string _signaturesStatus = "À jour (IA v4.8)";
        private string _lastScanText = "Il y a 12 min";
        private string _aiStatus = "Actif • Modèle local prêt";
        private int _aiConfidenceScore = 97;
        private string _aiLastDecision = "Blocage d'un script suspect d'élévation et vérification du jeton de récupération.";
        private bool _isScanning;
        private HourlyActivityPoint? _hoveredActivityPoint;

        /// <summary>
        /// Initialise le DashboardViewModel avec des données fictives cohérentes et démarre l'horloge en direct.
        /// Pourquoi : Permet d'afficher immédiatement un tableau de bord vivant et interactif dès l'ouverture.
        /// </summary>
        public DashboardViewModel()
        {
            ActivityPoints24h = new ObservableCollection<HourlyActivityPoint>();
            RecentEvents = new ObservableCollection<RecentSecurityEvent>();

            QuickScanCommand = new DashboardRelayCommand(
                execute: _ => StartQuickScan(),
                canExecute: _ => !IsScanning);

            LoadInitialMockData();

            // Configuration de l'horloge en direct (mise à jour toutes les secondes).
            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += (_, _) =>
            {
                LiveClockText = DateTime.Now.ToString("dddd dd MMMM yyyy • HH:mm:ss");
            };
            _clockTimer.Start();

            // Configuration du minuteur de simulation pour le bouton principal "Analyse rapide".
            _scanSimulationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1400)
            };
            _scanSimulationTimer.Tick += (_, _) => CompleteQuickScan();
        }

        /// <summary>
        /// Événement standard requis par INotifyPropertyChanged pour avertir WPF d'un changement de propriété.
        /// Pourquoi : Permet la mise à jour automatique des contrôles liés par Binding sans toucher au code visuel.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Nom de l'application affiché dans l'en-tête.
        /// Pourquoi : Identifie clairement le produit sur le tableau de bord.
        /// </summary>
        public string AppName
        {
            get => _appName;
            private set => SetProperty(ref _appName, value);
        }

        /// <summary>
        /// Sous-titre descriptif de l'antivirus IA et de l'aide à la récupération d'accès.
        /// Pourquoi : Contextualise la mission du tableau de bord dès le premier coup d'œil.
        /// </summary>
        public string AppTagline
        {
            get => _appTagline;
            private set => SetProperty(ref _appTagline, value);
        }

        /// <summary>
        /// Chaîne formatée représentant l'horloge en direct dans l'en-tête.
        /// Pourquoi : Offre un repère temporel continu à l'utilisateur.
        /// </summary>
        public string LiveClockText
        {
            get => _liveClockText;
            private set => SetProperty(ref _liveClockText, value);
        }

        /// <summary>
        /// État de protection actuel (Protégé, Attention ou Menace).
        /// Pourquoi : Pilote l'indicateur circulaire et les couleurs d'état de la carte principale.
        /// </summary>
        public ProtectionLevel ProtectionState
        {
            get => _protectionState;
            private set => SetProperty(ref _protectionState, value);
        }

        /// <summary>
        /// Libellé principal de l'état de protection.
        /// Pourquoi : Affiche clairement le niveau de sécurité courant.
        /// </summary>
        public string ProtectionTitle
        {
            get => _protectionTitle;
            private set => SetProperty(ref _protectionTitle, value);
        }

        /// <summary>
        /// Description courte associée à l'état de protection.
        /// Pourquoi : Explique brièvement l'état de la machine et de la récupération d'accès.
        /// </summary>
        public string ProtectionSubtitle
        {
            get => _protectionSubtitle;
            private set => SetProperty(ref _protectionSubtitle, value);
        }

        /// <summary>
        /// Nombre total de fichiers analysés (donnée fictive).
        /// Pourquoi : Alimente la première carte de statistique.
        /// </summary>
        public int FilesScannedCount
        {
            get => _filesScannedCount;
            private set => SetProperty(ref _filesScannedCount, value);
        }

        /// <summary>
        /// Nombre de menaces bloquées (donnée fictive).
        /// Pourquoi : Alimente la deuxième carte de statistique.
        /// </summary>
        public int ThreatsBlockedCount
        {
            get => _threatsBlockedCount;
            private set => SetProperty(ref _threatsBlockedCount, value);
        }

        /// <summary>
        /// État des signatures et du moteur IA (donnée fictive).
        /// Pourquoi : Alimente la troisième carte de statistique.
        /// </summary>
        public string SignaturesStatus
        {
            get => _signaturesStatus;
            private set => SetProperty(ref _signaturesStatus, value);
        }

        /// <summary>
        /// Indication relative à la dernière analyse effectuée (donnée fictive).
        /// Pourquoi : Alimente la quatrième carte de statistique.
        /// </summary>
        public string LastScanText
        {
            get => _lastScanText;
            private set => SetProperty(ref _lastScanText, value);
        }

        /// <summary>
        /// Statut courant du module d'intelligence artificielle.
        /// Pourquoi : Indique si l'IA est en veille active ou en cours d'évaluation.
        /// </summary>
        public string AiStatus
        {
            get => _aiStatus;
            private set => SetProperty(ref _aiStatus, value);
        }

        /// <summary>
        /// Score de confiance de l'IA exprimé en pourcentage (0 à 100).
        /// Pourquoi : Quantifie la fiabilité perçue des analyses et décisions de l'IA.
        /// </summary>
        public int AiConfidenceScore
        {
            get => _aiConfidenceScore;
            private set => SetProperty(ref _aiConfidenceScore, value);
        }

        /// <summary>
        /// Dernière décision prise ou recommandée par l'IA.
        /// Pourquoi : Donne de la transparence sur le comportement de l'IA (menaces et récupération d'accès).
        /// </summary>
        public string AiLastDecision
        {
            get => _aiLastDecision;
            private set => SetProperty(ref _aiLastDecision, value);
        }

        /// <summary>
        /// Indique si une simulation d'analyse rapide est en cours.
        /// Pourquoi : Permet d'animer le bouton principal et de désactiver les clics multiples pendant la simulation.
        /// </summary>
        public bool IsScanning
        {
            get => _isScanning;
            private set
            {
                if (SetProperty(ref _isScanning, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>
        /// Point horaire actuellement survolé sur la courbe des 24 dernières heures.
        /// Pourquoi : Permet d'afficher ou masquer l'infobulle détaillée de manière réactive.
        /// </summary>
        public HourlyActivityPoint? HoveredActivityPoint
        {
            get => _hoveredActivityPoint;
            set => SetProperty(ref _hoveredActivityPoint, value);
        }

        /// <summary>
        /// Série fictive d'activité sur les 24 dernières heures.
        /// Pourquoi : Alimente la courbe d'activité et l'interaction au survol.
        /// </summary>
        public ObservableCollection<HourlyActivityPoint> ActivityPoints24h { get; }

        /// <summary>
        /// Liste fictive des événements récents avec niveau de gravité.
        /// Pourquoi : Alimente le flux d'événements récents du tableau de bord.
        /// </summary>
        public ObservableCollection<RecentSecurityEvent> RecentEvents { get; }

        /// <summary>
        /// Commande principale déclenchant l'action "Analyse rapide".
        /// Pourquoi : Connecte le bouton principal du tableau de bord à la logique du ViewModel.
        /// </summary>
        public ICommand QuickScanCommand { get; }

        /// <summary>
        /// Charge un jeu complet de données fictives réalistes pour l'initialisation.
        /// Pourquoi : Assure un affichage riche et cohérent sans dépendre d'un moteur antivirus réel.
        /// </summary>
        private void LoadInitialMockData()
        {
            // Série fictive de 24 points (de -23h à l'heure actuelle).
            double[] mockValues =
            [
                24, 19, 15, 12, 14, 22, 38, 64,
                78, 85, 72, 69, 81, 90, 76, 63,
                58, 74, 88, 67, 52, 44, 36, 31
            ];

            ActivityPoints24h.Clear();
            DateTime now = DateTime.Now;
            for (int i = 0; i < mockValues.Length; i++)
            {
                int hoursAgo = mockValues.Length - 1 - i;
                string hourLabel = now.AddHours(-hoursAgo).ToString("HH:mm");
                string details = $"Analyse comportementale IA : {mockValues[i] * 18:N0} vérifications • Clé secours vérifiée";
                ActivityPoints24h.Add(new HourlyActivityPoint(hourLabel, mockValues[i], details));
            }

            RecentEvents.Clear();
            RecentEvents.Add(new RecentSecurityEvent(
                "Aujourd'hui, 05:21",
                "Clé de récupération d'accès validée",
                "Jeton local chiffré prêt en cas d'oubli de mot de passe.",
                EventSeverity.Info));
            RecentEvents.Add(new RecentSecurityEvent(
                "Aujourd'hui, 04:48",
                "Tentative de connexion répétée neutralisée",
                "Limitation automatique activée sur l'écran de session.",
                EventSeverity.Avertissement));
            RecentEvents.Add(new RecentSecurityEvent(
                "Hier, 23:14",
                "Charge utile scriptée bloquée en mémoire",
                "Isolée par le moteur heuristique IA avant exécution.",
                EventSeverity.Critique));
            RecentEvents.Add(new RecentSecurityEvent(
                "Hier, 19:05",
                "Base de signatures IA synchronisée",
                "Nouveaux profils d'hameçonnage et de rançongiciel chargés.",
                EventSeverity.Info));
        }

        /// <summary>
        /// Démarre une simulation d'analyse rapide fictive.
        /// Pourquoi : Offre un retour visuel immédiat et crédible lorsque l'utilisateur clique sur "Analyse rapide".
        /// </summary>
        private void StartQuickScan()
        {
            if (IsScanning)
            {
                return;
            }

            IsScanning = true;
            AiStatus = "Analyse rapide en cours...";
            ProtectionSubtitle = "Inspection des processus clés, des alertes récentes et du module d'accès.";
            _scanSimulationTimer.Start();
        }

        /// <summary>
        /// Termine la simulation d'analyse rapide et met à jour les statistiques, l'IA et les événements fictifs.
        /// Pourquoi : Démontre l'impact des bindings WPF après une action utilisateur sans logique antivirus réelle.
        /// </summary>
        private void CompleteQuickScan()
        {
            _scanSimulationTimer.Stop();

            int addedFiles = _random.Next(320, 1450);
            FilesScannedCount += addedFiles;
            LastScanText = "À l'instant";

            // Alterne de manière contrôlée entre Protégé et Attention (ou Menace ponctuelle) pour illustrer l'UI.
            int roll = _random.Next(0, 10);
            if (roll <= 6)
            {
                ProtectionState = ProtectionLevel.Protege;
                ProtectionTitle = "Protégé";
                ProtectionSubtitle = "Aucune menace active détectée • Récupération de session opérationnelle.";
                AiConfidenceScore = _random.Next(95, 100);
                AiLastDecision = "Validation complète des services critiques et maintien du mode protégé.";
                RecentEvents.Insert(0, new RecentSecurityEvent(
                    DateTime.Now.ToString("HH:mm:ss"),
                    $"Analyse rapide terminée (+{addedFiles:N0} fichiers)",
                    "Aucun anomalie détectée sur le poste local.",
                    EventSeverity.Info));
            }
            else if (roll <= 8)
            {
                ProtectionState = ProtectionLevel.Attention;
                ProtectionTitle = "Attention";
                ProtectionSubtitle = "Un processus inhabituel nécessite une vigilance accrue.";
                AiConfidenceScore = _random.Next(84, 92);
                AiLastDecision = "Mise sous surveillance renforcée d'un processus à comportement atypique.";
                RecentEvents.Insert(0, new RecentSecurityEvent(
                    DateTime.Now.ToString("HH:mm:ss"),
                    "Comportement système atypique observé",
                    "Surveillance renforcée activée par l'analyse IA.",
                    EventSeverity.Avertissement));
            }
            else
            {
                ThreatsBlockedCount += 1;
                ProtectionState = ProtectionLevel.Menace;
                ProtectionTitle = "Menace";
                ProtectionSubtitle = "Tentative malveillante interceptée et bloquée par l'IA.";
                AiConfidenceScore = _random.Next(91, 98);
                AiLastDecision = "Blocage immédiat d'une signature suspecte et protection du canal de récupération.";
                RecentEvents.Insert(0, new RecentSecurityEvent(
                    DateTime.Now.ToString("HH:mm:ss"),
                    "Menace simulée interceptée et bloquée",
                    "Action isolée automatiquement par le bouclier IA.",
                    EventSeverity.Critique));
            }

            // Conserve une liste compacte et lisible des événements récents.
            while (RecentEvents.Count > 6)
            {
                RecentEvents.RemoveAt(RecentEvents.Count - 1);
            }

            // Met à jour le dernier point de la courbe 24h pour refléter l'activité simulée.
            if (ActivityPoints24h.Count > 0)
            {
                int lastIndex = ActivityPoints24h.Count - 1;
                double updatedValue = Math.Clamp(ActivityPoints24h[lastIndex].ActivityValue + _random.Next(8, 22), 15, 100);
                ActivityPoints24h[lastIndex] = new HourlyActivityPoint(
                    DateTime.Now.ToString("HH:mm"),
                    updatedValue,
                    $"Pic d'analyse rapide : {updatedValue * 20:N0} vérifications simulées");
                OnPropertyChanged(nameof(ActivityPoints24h));
            }

            AiStatus = "Actif • Surveillance continue";
            IsScanning = false;
        }

        /// <summary>
        /// Met à jour un champ et déclenche PropertyChanged uniquement si la valeur a changé.
        /// Pourquoi : Évite les notifications superflues et factorise la logique INotifyPropertyChanged.
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
        /// Déclenche manuellement PropertyChanged pour une propriété donnée.
        /// Pourquoi : Permet de notifier la vue lorsqu'une collection ou un élément interne est mis à jour.
        /// </summary>
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Implémentation légère de ICommand pour relier les boutons WPF aux méthodes du ViewModel.
        /// Pourquoi : Évite toute dépendance externe tout en respectant le contrat CanExecute / Execute de WPF.
        /// </summary>
        private sealed class DashboardRelayCommand : ICommand
        {
            private readonly Action<object?> _execute;
            private readonly Predicate<object?>? _canExecute;

            public DashboardRelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
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

            public void Execute(object? parameter) => _execute(parameter);
        }
    }
}