using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScamShield.Core.Models;

namespace ScamShield.Agent;

/// <summary>
/// Point d'entrée unique des fichiers à analyser : filtre, attend la fin d'écriture,
/// puis place la demande dans la file. Ne lève jamais d'exception vers l'appelant.
/// </summary>
public sealed class FileSubmitter
{
    private static readonly string[] TemporaryExtensions =
        [".crdownload", ".part", ".partial", ".tmp", ".download", ".opdownload"];

    private readonly ScanQueue _queue;
    private readonly AgentOptions _options;
    private readonly ILogger<FileSubmitter> _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly SemaphoreSlim _gate;
    private readonly string _quarantineDirectory;

    public FileSubmitter(
        ScanQueue queue,
        IOptions<AgentOptions> options,
        IHostApplicationLifetime lifetime,
        ILogger<FileSubmitter> logger)
    {
        _queue = queue;
        _options = options.Value;
        _lifetime = lifetime;
        _logger = logger;
        _gate = new SemaphoreSlim(Math.Max(1, _options.MaxConcurrentStabilizations));
        _quarantineDirectory = Path.GetFullPath(AgentPaths.Quarantine(_options));
    }

    /// <summary>Soumet un fichier en arrière-plan (utilisable depuis un gestionnaire d'événement).</summary>
    public void Fire(string path, ScanTrigger trigger)
    {
        var token = _lifetime.ApplicationStopping;
        _ = Task.Run(() => SubmitAsync(path, trigger, token), CancellationToken.None);
    }

    public async Task SubmitAsync(string path, ScanTrigger trigger, CancellationToken cancellationToken)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            if (IsIgnored(fullPath) || !_queue.TryReserve(fullPath))
            {
                return;
            }

            var enqueued = false;
            try
            {
                bool ready;
                await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    ready = await FileStabilizer.WaitUntilReadyAsync(
                        fullPath,
                        TimeSpan.FromSeconds(_options.StabilityTimeoutSeconds),
                        cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    _gate.Release();
                }

                if (!ready)
                {
                    _logger.LogWarning("Fichier ignoré (disparu, verrouillé ou inaccessible) : {Path}", fullPath);
                    return;
                }

                await _queue.EnqueueAsync(new ScanRequest(fullPath, trigger), cancellationToken).ConfigureAwait(false);
                enqueued = true;
            }
            finally
            {
                if (!enqueued)
                {
                    _queue.Release(fullPath);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Arrêt du service : normal.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Échec de la soumission du fichier {Path}", path);
        }
    }

    private bool IsIgnored(string fullPath)
    {
        if (fullPath.StartsWith(_quarantineDirectory, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (Directory.Exists(fullPath))
        {
            return true;
        }

        var extension = Path.GetExtension(fullPath);
        return TemporaryExtensions.Any(e => e.Equals(extension, StringComparison.OrdinalIgnoreCase));
    }
}
