namespace ScamShield.Agent;

/// <summary>Réglages de l'Agent (section « Agent » de appsettings.json).</summary>
public sealed class AgentOptions
{
    /// <summary>Dossier de données : base des menaces, quarantaine, journaux.</summary>
    public string DataDirectory { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ScamShield");

    /// <summary>
    /// Dossiers surveillés. Si la liste est vide, l'Agent surveille le dossier Téléchargements
    /// de chaque profil présent dans C:\Users (indispensable : le service tourne sous SYSTEM).
    /// </summary>
    public List<string> WatchedFolders { get; set; } = [];

    /// <summary>Taille maximale de la file d'attente (au-delà, la détection attend : pas de perte, pas d'explosion mémoire).</summary>
    public int QueueCapacity { get; set; } = 500;

    /// <summary>Nom du named pipe utilisé par l'interface.</summary>
    public string PipeName { get; set; } = "ScamShield.Agent";

    /// <summary>Durée maximale d'attente de la fin d'écriture d'un fichier.</summary>
    public int StabilityTimeoutSeconds { get; set; } = 120;

    /// <summary>Nombre maximal de fichiers dont on attend la fin d'écriture en même temps.</summary>
    public int MaxConcurrentStabilizations { get; set; } = 8;

    /// <summary>Analyse rapide des supports amovibles au branchement.</summary>
    public bool ScanRemovableDrives { get; set; } = true;
}

/// <summary>Chemins dérivés des options.</summary>
public static class AgentPaths
{
    public static string Quarantine(AgentOptions o) => Path.Combine(o.DataDirectory, "Quarantine");

    public static string Logs(AgentOptions o) => Path.Combine(o.DataDirectory, "Logs");

    public static string ThreatDatabase(AgentOptions o) => Path.Combine(o.DataDirectory, "threats.db");

    /// <summary>Dossiers à surveiller (configurés, ou Téléchargements de tous les profils).</summary>
    public static IReadOnlyList<string> ResolveWatchedFolders(AgentOptions o)
    {
        if (o.WatchedFolders.Count > 0)
        {
            return o.WatchedFolders;
        }

        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var root = Path.GetPathRoot(windows) ?? @"C:\";
        var users = Path.Combine(root, "Users");
        var result = new List<string>();

        try
        {
            foreach (var profile in Directory.EnumerateDirectories(users))
            {
                var downloads = Path.Combine(profile, "Downloads");
                if (Directory.Exists(downloads))
                {
                    result.Add(downloads);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Pas de dossier Users lisible : la liste restera vide, l'Agent le journalise.
        }

        return result;
    }
}
