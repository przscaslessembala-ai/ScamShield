using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ScamShield.UI.Services
{
    /// <summary>
    /// Implémentation fictive (mock) de IAgentClient pour ScamShield.UI.
    /// Pourquoi : Permet de faire tourner l'interface complète (alertes, quarantaine, paramètres, scan)
    /// avec des données réalistes avant le branchement réel au serveur named pipe de Jean.
    /// </summary>
    public sealed class MockAgentClient : IAgentClient
    {
        // Verrou léger pour protéger l'accès aux collections en mémoire.
        private readonly object _syncLock = new();

        // Générateur pseudo-aléatoire pour varier les scénarios simulés lors d'une analyse rapide.
        private readonly Random _random = new();

        private readonly List<AgentAlertReport> _alerts;
        private readonly List<AgentQuarantineEntry> _quarantineItems;
        private AgentProtectionConfiguration _configuration;
        private AgentProtectionStatus _protectionStatus;
        private int _scanCounter;

        /// <summary>
        /// Initialise MockAgentClient avec des alertes (Sain, Suspect, Dangereux),
        /// un élément en quarantaine et une configuration de départ cohérente.
        /// Pourquoi : Offre immédiatement un jeu de données complet et compréhensible sans dépendre d'un service Windows.
        /// </summary>
        public MockAgentClient()
        {
            _protectionStatus = AgentProtectionStatus.Protege;

            _configuration = new AgentProtectionConfiguration(
                watchedFolders:
                [
                    @"C:\Users\Public\Downloads",
                    @"C:\Users\Public\Desktop"
                ],
                sensitivityLevel: "Équilibrée",
                allowList:
                [
                    @"C:\Program Files\ScamShield\RecoveryTool.exe"
                ]);

            _alerts =
            [
                new AgentAlertReport(
                    alertId: "ALT-101",
                    fileName: "facture_urgente_2026.exe",
                    filePath: @"C:\Users\Public\Downloads\facture_urgente_2026.exe",
                    riskScore: 0.91,
                    zone: AgentThreatZone.Dangereux,
                    reasons:
                    [
                        "Le fichier tente de modifier des zones sensibles du système sans éditeur reconnu.",
                        "Son comportement ressemble à un programme qui cherche à masquer son action."
                    ],
                    detectedAt: DateTime.Now.AddMinutes(-18),
                    isQuarantined: true),

                new AgentAlertReport(
                    alertId: "ALT-102",
                    fileName: "utilitaire_mot_de_passe_beta.zip",
                    filePath: @"C:\Users\Public\Downloads\utilitaire_mot_de_passe_beta.zip",
                    riskScore: 0.54,
                    zone: AgentThreatZone.Suspect,
                    reasons:
                    [
                        "Archive compressée contenant un script inhabituel.",
                        "Aucun antécédent dangereux connu, mais une vérification est conseillée."
                    ],
                    detectedAt: DateTime.Now.AddHours(-1),
                    isQuarantined: false),

                new AgentAlertReport(
                    alertId: "ALT-103",
                    fileName: "installateur_pilote_audio.msi",
                    filePath: @"C:\Users\Public\Downloads\installateur_pilote_audio.msi",
                    riskScore: 0.08,
                    zone: AgentThreatZone.Sain,
                    reasons:
                    [
                        "Signature numérique valide et éditeur vérifié.",
                        "Aucun comportement à risque détecté lors de l'analyse."
                    ],
                    detectedAt: DateTime.Now.AddHours(-3),
                    isQuarantined: false)
            ];

            _quarantineItems =
            [
                new AgentQuarantineEntry(
                    quarantineId: "QRN-201",
                    fileName: "facture_urgente_2026.exe",
                    originalPath: @"C:\Users\Public\Downloads\facture_urgente_2026.exe",
                    riskScore: 0.91,
                    summaryReason: "Fichier dangereux isolé automatiquement pour protéger votre PC.",
                    quarantinedAt: DateTime.Now.AddMinutes(-18))
            ];
        }

        /// <summary>
        /// Événement déclenché lorsqu'une nouvelle alerte est produite.
        /// Pourquoi : Permet à l'UI de mettre à jour les vues et de déclencher une notification Windows.
        /// </summary>
        public event EventHandler<AgentAlertReport>? ThreatAlertReceived;

        /// <summary>
        /// Retourne l'état actuel de protection (Protégé ou À risque).
        /// Pourquoi : Reflète la présence ou non de menaces dangereuses non mises en quarantaine.
        /// </summary>
        public Task<AgentProtectionStatus> GetProtectionStatusAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_syncLock)
            {
                return Task.FromResult(_protectionStatus);
            }
        }

        /// <summary>
        /// Retourne la liste courante des alertes simulées.
        /// Pourquoi : Alimente la vue des alertes par zone (Sain, Suspect, Dangereux).
        /// </summary>
        public Task<IReadOnlyList<AgentAlertReport>> GetAlertsAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_syncLock)
            {
                IReadOnlyList<AgentAlertReport> snapshot = _alerts.ToList();
                return Task.FromResult(snapshot);
            }
        }

        /// <summary>
        /// Retourne la liste des éléments placés en quarantaine simulée.
        /// Pourquoi : Fournit les éléments à restaurer ou supprimer sur l'écran de quarantaine.
        /// </summary>
        public Task<IReadOnlyList<AgentQuarantineEntry>> GetQuarantineItemsAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_syncLock)
            {
                IReadOnlyList<AgentQuarantineEntry> snapshot = _quarantineItems.ToList();
                return Task.FromResult(snapshot);
            }
        }

        /// <summary>
        /// Simule la restauration d'un fichier depuis la quarantaine.
        /// Pourquoi : Permet de tester la restauration côté UI et d'actualiser l'état des alertes.
        /// </summary>
        public Task<bool> RestoreQuarantineItemAsync(string quarantineId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(quarantineId))
            {
                return Task.FromResult(false);
            }

            lock (_syncLock)
            {
                AgentQuarantineEntry? entry = _quarantineItems.FirstOrDefault(x => x.QuarantineId == quarantineId);
                if (entry is null)
                {
                    return Task.FromResult(false);
                }

                _quarantineItems.Remove(entry);

                for (int i = 0; i < _alerts.Count; i++)
                {
                    AgentAlertReport current = _alerts[i];
                    if (string.Equals(current.FilePath, entry.OriginalPath, StringComparison.OrdinalIgnoreCase))
                    {
                        _alerts[i] = new AgentAlertReport(
                            current.AlertId,
                            current.FileName,
                            current.FilePath,
                            current.RiskScore,
                            current.Zone,
                            current.Reasons,
                            current.DetectedAt,
                            isQuarantined: false);
                    }
                }

                RecalculateProtectionStatusLocked();
                return Task.FromResult(true);
            }
        }

        /// <summary>
        /// Simule la suppression définitive d'un fichier en quarantaine.
        /// Pourquoi : Permet de valider la suppression d'un élément isolé depuis l'UI.
        /// </summary>
        public Task<bool> DeleteQuarantineItemAsync(string quarantineId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(quarantineId))
            {
                return Task.FromResult(false);
            }

            lock (_syncLock)
            {
                AgentQuarantineEntry? entry = _quarantineItems.FirstOrDefault(x => x.QuarantineId == quarantineId);
                if (entry is null)
                {
                    return Task.FromResult(false);
                }

                _quarantineItems.Remove(entry);
                RecalculateProtectionStatusLocked();
                return Task.FromResult(true);
            }
        }

        /// <summary>
        /// Retourne une copie des paramètres de surveillance actuels.
        /// Pourquoi : Protège l'état interne du mock contre des modifications non enregistrées.
        /// </summary>
        public Task<AgentProtectionConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_syncLock)
            {
                var copy = new AgentProtectionConfiguration(
                    _configuration.WatchedFolders.ToList(),
                    _configuration.SensitivityLevel,
                    _configuration.AllowList.ToList());
                return Task.FromResult(copy);
            }
        }

        /// <summary>
        /// Enregistre les paramètres de dossiers surveillés, sensibilité et liste blanche.
        /// Pourquoi : Conserve les réglages choisis par l'utilisateur dans l'écran Paramètres.
        /// </summary>
        public Task<bool> SaveConfigurationAsync(AgentProtectionConfiguration configuration, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (configuration is null)
            {
                return Task.FromResult(false);
            }

            lock (_syncLock)
            {
                List<string> cleanedFolders = configuration.WatchedFolders
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(p => p.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                List<string> cleanedAllowList = configuration.AllowList
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(p => p.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                string sensitivity = string.IsNullOrWhiteSpace(configuration.SensitivityLevel)
                    ? "Équilibrée"
                    : configuration.SensitivityLevel.Trim();

                _configuration = new AgentProtectionConfiguration(cleanedFolders, sensitivity, cleanedAllowList);
                return Task.FromResult(true);
            }
        }

        /// <summary>
        /// Simule une analyse rapide produisant un fichier Sain, Suspect ou Dangereux,
        /// met à jour la quarantaine si nécessaire et déclenche ThreatAlertReceived.
        /// Pourquoi : Permet de tester l'affichage des alertes, des zones et des notifications Windows.
        /// </summary>
        public Task<AgentAlertReport> TriggerQuickScanAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            AgentAlertReport generatedAlert;
            lock (_syncLock)
            {
                _scanCounter++;
                int roll = _random.Next(0, 10);

                if (roll <= 4)
                {
                    string fileName = $"document_controle_{_scanCounter}.pdf";
                    generatedAlert = new AgentAlertReport(
                        alertId: $"ALT-{110 + _scanCounter}",
                        fileName: fileName,
                        filePath: $@"C:\Users\Public\Downloads\{fileName}",
                        riskScore: 0.12,
                        zone: AgentThreatZone.Sain,
                        reasons:
                        [
                            "Document standard sans code exécutable caché.",
                            "Aucun comportement à risque repéré."
                        ],
                        detectedAt: DateTime.Now,
                        isQuarantined: false);
                }
                else if (roll <= 7)
                {
                    string fileName = $"archive_inconnue_{_scanCounter}.zip";
                    generatedAlert = new AgentAlertReport(
                        alertId: $"ALT-{110 + _scanCounter}",
                        fileName: fileName,
                        filePath: $@"C:\Users\Public\Downloads\{fileName}",
                        riskScore: 0.58,
                        zone: AgentThreatZone.Suspect,
                        reasons:
                        [
                            "Archive compressée contenant un élément inhabituel.",
                            "Vérifiez la provenance du fichier avant de l'ouvrir."
                        ],
                        detectedAt: DateTime.Now,
                        isQuarantined: false);
                }
                else
                {
                    string fileName = $"script_bloque_{_scanCounter}.exe";
                    string filePath = $@"C:\Users\Public\Downloads\{fileName}";
                    double score = 0.89;

                    generatedAlert = new AgentAlertReport(
                        alertId: $"ALT-{110 + _scanCounter}",
                        fileName: fileName,
                        filePath: filePath,
                        riskScore: score,
                        zone: AgentThreatZone.Dangereux,
                        reasons:
                        [
                            "Comportement dangereux détecté dans un fichier téléchargé.",
                            "Le fichier a été placé en quarantaine automatiquement."
                        ],
                        detectedAt: DateTime.Now,
                        isQuarantined: true);

                    _quarantineItems.Insert(0, new AgentQuarantineEntry(
                        quarantineId: $"QRN-{210 + _scanCounter}",
                        fileName: fileName,
                        originalPath: filePath,
                        riskScore: score,
                        summaryReason: "Menace bloquée et isolée automatiquement.",
                        quarantinedAt: DateTime.Now));
                }

                _alerts.Insert(0, generatedAlert);
                RecalculateProtectionStatusLocked();
            }

            ThreatAlertReceived?.Invoke(this, generatedAlert);
            return Task.FromResult(generatedAlert);
        }

        /// <summary>
        /// Recalcule l'état global de protection selon la présence de fichiers dangereux non mis en quarantaine.
        /// Pourquoi : Maintient la cohérence entre les alertes et l'indicateur Protégé / À risque.
        /// </summary>
        private void RecalculateProtectionStatusLocked()
        {
            bool hasUnquarantinedDanger = _alerts.Any(a => a.Zone == AgentThreatZone.Dangereux && !a.IsQuarantined);
            _protectionStatus = hasUnquarantinedDanger ? AgentProtectionStatus.ARisque : AgentProtectionStatus.Protege;
        }
    }
}