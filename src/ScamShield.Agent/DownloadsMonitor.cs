using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScamShield.Core.Models;

namespace ScamShield.Agent;

/// <summary>Surveille les dossiers de téléchargement (FileSystemWatcher) et soumet les nouveaux fichiers.</summary>
public sealed class DownloadsMonitor(
    IOptions<AgentOptions> options,
    FileSubmitter submitter,
    ILogger<DownloadsMonitor> logger) : BackgroundService
{
    private readonly List<FileSystemWatcher> _watchers = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var folders = AgentPaths.ResolveWatchedFolders(options.Value);
        if (folders.Count == 0)
        {
            logger.LogWarning("Aucun dossier à surveiller : renseignez Agent:WatchedFolders dans appsettings.json");
        }

        foreach (var folder in folders)
        {
            if (!Directory.Exists(folder))
            {
                logger.LogWarning("Dossier surveillé introuvable : {Folder}", folder);
                continue;
            }

            try
            {
                var watcher = new FileSystemWatcher(folder)
                {
                    IncludeSubdirectories = false,
                    InternalBufferSize = 64 * 1024,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                };

                watcher.Created += (_, e) => submitter.Fire(e.FullPath, ScanTrigger.Download);
                watcher.Renamed += (_, e) => submitter.Fire(e.FullPath, ScanTrigger.Download); // .crdownload → nom final
                watcher.Error += (_, e) => logger.LogWarning(e.GetException(), "Erreur de surveillance sur {Folder} (événements perdus ?)", folder);
                watcher.EnableRaisingEvents = true;

                _watchers.Add(watcher);
                logger.LogInformation("Surveillance du dossier {Folder}", folder);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Impossible de surveiller {Folder}", folder);
            }
        }

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Arrêt du service : normal.
        }
    }

    public override void Dispose()
    {
        foreach (var watcher in _watchers)
        {
            watcher.Dispose();
        }

        base.Dispose();
    }
}
