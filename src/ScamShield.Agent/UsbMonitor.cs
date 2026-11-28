using System.Management;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScamShield.Core.Models;

namespace ScamShield.Agent;

/// <summary>
/// Détecte le branchement d'un volume (Win32_VolumeChangeEvent) et lance un scan rapide :
/// fichiers de la racine + fichiers à risque (.exe, .dll, .scr, .lnk, autorun.inf) sur 3 niveaux.
/// </summary>
public sealed class UsbMonitor(
    IOptions<AgentOptions> options,
    FileSubmitter submitter,
    ILogger<UsbMonitor> logger) : BackgroundService
{
    private const int MaxFilesPerDrive = 500;

    private static readonly HashSet<string> RiskyExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".scr", ".lnk", ".com", ".pif", ".bat", ".cmd", ".vbs", ".js", ".ps1",
    };

    private ManagementEventWatcher? _watcher;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.ScanRemovableDrives)
        {
            return;
        }

        try
        {
            // EventType 2 = arrivée d'un volume.
            _watcher = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2"));
            _watcher.EventArrived += (_, e) =>
            {
                var drive = e.NewEvent["DriveName"] as string;
                if (!string.IsNullOrWhiteSpace(drive))
                {
                    _ = Task.Run(() => ScanDriveAsync(drive, stoppingToken), CancellationToken.None);
                }
            };
            _watcher.Start();
            logger.LogInformation("Détection des supports amovibles activée");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Détection USB indisponible (WMI)");
            return;
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

    private async Task ScanDriveAsync(string driveName, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken); // laisse Windows finir le montage

            var drive = new DriveInfo(driveName);
            if (!drive.IsReady || drive.DriveType != DriveType.Removable)
            {
                return;
            }

            var root = drive.RootDirectory.FullName;
            logger.LogInformation("Support amovible détecté : {Drive}", root);

           var enumeration = new System.IO.EnumerationOptions
            {
                RecurseSubdirectories = true,
                MaxRecursionDepth = 3,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint,
            };

            var files = Directory.EnumerateFiles(root, "*", SearchOption.TopDirectoryOnly)
                .Concat(Directory.EnumerateFiles(root, "*", enumeration)
                    .Where(f => RiskyExtensions.Contains(Path.GetExtension(f))))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(MaxFilesPerDrive)
                .ToList();

            logger.LogInformation("Scan rapide de {Drive} : {Count} fichier(s)", root, files.Count);

            foreach (var file in files)
            {
                submitter.Fire(file, ScanTrigger.RemovableDrive); // la concurrence est limitée par le FileSubmitter
            }
        }
        catch (OperationCanceledException)
        {
            // Arrêt du service : normal.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Échec du scan du support {Drive}", driveName);
        }
    }

    public override void Dispose()
    {
        _watcher?.Stop();
        _watcher?.Dispose();
        base.Dispose();
    }
}
