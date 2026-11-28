using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ScamShield.UI.Services
{
    /// <summary>
    /// Représente les trois zones de décision affichées à l'utilisateur pour un fichier analysé.
    /// Pourquoi : Standardise les catégories (Sain, Suspect, Dangereux) entre l'Agent, la simulation mock et l'interface WPF.
    /// </summary>
    public enum AgentThreatZone
    {
        Sain,
        Suspect,
        Dangereux
    }

    /// <summary>
    /// Représente le niveau global de sécurité exposé par le service Agent.
    /// Pourquoi : Permet d'afficher clairement si le système est protégé ou à risque.
    /// </summary>
    public enum AgentProtectionStatus
    {
        Protege,
        ARisque
    }

    /// <summary>
    /// Représente un rapport d'alerte ou d'analyse envoyé par l'Agent vers l'interface utilisateur.
    /// Pourquoi : Transporte le chemin, le score (0.0 à 1.0), la zone et des explications simples sans jargon technique.
    /// </summary>
    public sealed class AgentAlertReport
    {
        public AgentAlertReport(
            string alertId,
            string fileName,
            string filePath,
            double riskScore,
            AgentThreatZone zone,
            IReadOnlyList<string> reasons,
            DateTime detectedAt,
            bool isQuarantined)
        {
            AlertId = alertId;
            FileName = fileName;
            FilePath = filePath;
            RiskScore = Math.Clamp(riskScore, 0.0, 1.0);
            Zone = zone;
            Reasons = reasons ?? Array.Empty<string>();
            DetectedAt = detectedAt;
            IsQuarantined = isQuarantined;
        }

        public string AlertId { get; }
        public string FileName { get; }
        public string FilePath { get; }
        public double RiskScore { get; }
        public AgentThreatZone Zone { get; }
        public IReadOnlyList<string> Reasons { get; }
        public DateTime DetectedAt { get; }
        public bool IsQuarantined { get; }
    }

    /// <summary>
    /// Représente un élément placé en quarantaine par le service Agent.
    /// Pourquoi : Fournit à l'écran de quarantaine les informations nécessaires pour restaurer ou supprimer un fichier.
    /// </summary>
    public sealed class AgentQuarantineEntry
    {
        public AgentQuarantineEntry(
            string quarantineId,
            string fileName,
            string originalPath,
            double riskScore,
            string summaryReason,
            DateTime quarantinedAt)
        {
            QuarantineId = quarantineId;
            FileName = fileName;
            OriginalPath = originalPath;
            RiskScore = Math.Clamp(riskScore, 0.0, 1.0);
            SummaryReason = summaryReason;
            QuarantinedAt = quarantinedAt;
        }

        public string QuarantineId { get; }
        public string FileName { get; }
        public string OriginalPath { get; }
        public double RiskScore { get; }
        public string SummaryReason { get; }
        public DateTime QuarantinedAt { get; }
    }

    /// <summary>
    /// Représente les paramètres de surveillance configurables depuis l'interface WPF.
    /// Pourquoi : Regroupe les dossiers surveillés, la sensibilité et la liste blanche dans un contrat clair.
    /// </summary>
    public sealed class AgentProtectionConfiguration
    {
        public AgentProtectionConfiguration(
            IReadOnlyList<string> watchedFolders,
            string sensitivityLevel,
            IReadOnlyList<string> allowList)
        {
            WatchedFolders = watchedFolders ?? Array.Empty<string>();
            SensitivityLevel = string.IsNullOrWhiteSpace(sensitivityLevel) ? "Équilibrée" : sensitivityLevel;
            AllowList = allowList ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> WatchedFolders { get; }
        public string SensitivityLevel { get; }
        public IReadOnlyList<string> AllowList { get; }
    }

    /// <summary>
    /// Contrat de communication entre l'interface WPF (ScamShield.UI) et le service Agent (ScamShield.Agent).
    /// Pourquoi : Découple l'interface du transport (mock local aujourd'hui, named pipe avec Jean ensuite)
    /// et permet de recevoir les alertes bloquées pour déclencher les notifications Windows.
    /// </summary>
    public interface IAgentClient
    {
        /// <summary>
        /// Déclenché lorsqu'une nouvelle alerte (notamment une menace bloquée) arrive depuis l'Agent.
        /// Pourquoi : Permet à l'UI de mettre à jour l'affichage et de déclencher une notification Windows.
        /// </summary>
        event EventHandler<AgentAlertReport>? ThreatAlertReceived;

        /// <summary>
        /// Récupère l'état actuel de protection (Protégé ou À risque).
        /// Pourquoi : Synchronise l'indicateur global de sécurité avec l'état du service.
        /// </summary>
        Task<AgentProtectionStatus> GetProtectionStatusAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Récupère la liste des alertes classées par zone (Sain, Suspect, Dangereux).
        /// Pourquoi : Alimente la vue d'alertes avec le score et les raisons en français simple.
        /// </summary>
        Task<IReadOnlyList<AgentAlertReport>> GetAlertsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Récupère les fichiers actuellement placés en quarantaine.
        /// Pourquoi : Alimente l'écran de quarantaine pour les actions de restauration ou de suppression.
        /// </summary>
        Task<IReadOnlyList<AgentQuarantineEntry>> GetQuarantineItemsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Restaure un fichier isolé vers son emplacement d'origine.
        /// Pourquoi : Donne à l'utilisateur un contrôle réversible en cas de faux positif.
        /// </summary>
        Task<bool> RestoreQuarantineItemAsync(string quarantineId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Supprime définitivement un fichier isolé en quarantaine.
        /// Pourquoi : Permet d'éliminer une menace confirmée après examen par l'utilisateur.
        /// </summary>
        Task<bool> DeleteQuarantineItemAsync(string quarantineId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Récupère les paramètres de surveillance actuels.
        /// Pourquoi : Charge les dossiers surveillés, la sensibilité et la liste blanche dans l'écran Paramètres.
        /// </summary>
        Task<AgentProtectionConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Enregistre les paramètres de surveillance mis à jour par l'utilisateur.
        /// Pourquoi : Transmet les préférences de dossiers, sensibilité et liste blanche au service.
        /// </summary>
        Task<bool> SaveConfigurationAsync(AgentProtectionConfiguration configuration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Déclenche une analyse rapide simulée ou demandée à l'Agent.
        /// Pourquoi : Permet de tester le trajet complet (analyse, score, zone, alerte) depuis l'UI.
        /// </summary>
        Task<AgentAlertReport> TriggerQuickScanAsync(CancellationToken cancellationToken = default);
    }
}