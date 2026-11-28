using Microsoft.Extensions.Hosting.WindowsServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScamShield.Core.Abstractions;
using ScamShield.Core.Models;

namespace ScamShield.Agent;

/// <summary>
/// Quarantaine : le fichier est déplacé sous un nom aléatoire sans extension exécutable (« .quarantine »),
/// ses informations de restauration sont conservées à côté (« .json »).
/// Sous service Windows, le dossier n'est accessible qu'à SYSTEM et aux administrateurs.
/// </summary>
public sealed class QuarantineService : IQuarantineService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _directory;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly UserAllowList _allowList;
    private readonly ILogger<QuarantineService> _logger;

    public QuarantineService(IOptions<AgentOptions> options, UserAllowList allowList, ILogger<QuarantineService> logger)
    {
        _allowList = allowList;
        _logger = logger;
        _directory = AgentPaths.Quarantine(options.Value);
        EnsureDirectory(_directory, restrictAccess: WindowsServiceHelpers.IsWindowsService());
    }

    public async Task<QuarantineEntry> QuarantineAsync(ScanResult result, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var id = Guid.NewGuid();
            var storedPath = StoredPath(id);

            await MoveWithRetryAsync(result.FilePath, storedPath, cancellationToken).ConfigureAwait(false);

            var entry = new QuarantineEntry(
                id,
                result.FilePath,
                storedPath,
                result.Sha256,
                result.Score,
                result.Reasons,
                DateTimeOffset.UtcNow);

            try
            {
                await WriteMetadataAsync(entry, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Sans métadonnées, la restauration serait impossible : on remet le fichier en place.
                File.Move(storedPath, result.FilePath);
                throw;
            }

            _logger.LogWarning("Fichier mis en quarantaine : {Path} (score {Score:0.00})", result.FilePath, result.Score);
            return entry;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RestoreAsync(Guid entryId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var entry = await ReadMetadataAsync(entryId, cancellationToken).ConfigureAwait(false);
            var target = UniqueTarget(entry.OriginalPath);

            var parent = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(parent))
            {
                Directory.CreateDirectory(parent);
            }

            _allowList.Allow(entry.Sha256);
            File.Move(StoredPath(entryId), target);
            File.Delete(MetadataPath(entryId));

            _logger.LogInformation("Fichier restauré : {Path}", target);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DeleteAsync(Guid entryId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(MetadataPath(entryId)) && !File.Exists(StoredPath(entryId)))
            {
                throw new FileNotFoundException("Entrée de quarantaine introuvable.");
            }

            File.Delete(StoredPath(entryId));
            File.Delete(MetadataPath(entryId));
            _logger.LogInformation("Fichier supprimé définitivement de la quarantaine : {Id}", entryId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<QuarantineEntry>> ListAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var entries = new List<QuarantineEntry>();
            foreach (var file in Directory.EnumerateFiles(_directory, "*.json"))
            {
                try
                {
                    await using var stream = File.OpenRead(file);
                    var entry = await JsonSerializer.DeserializeAsync<QuarantineEntry>(stream, JsonOptions, cancellationToken)
                        .ConfigureAwait(false);
                    if (entry is not null)
                    {
                        entries.Add(entry);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Métadonnées de quarantaine illisibles : {File}", file);
                }
            }

            return entries.OrderByDescending(e => e.QuarantinedAt).ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    private string StoredPath(Guid id) => Path.Combine(_directory, id.ToString("N") + ".quarantine");

    private string MetadataPath(Guid id) => Path.Combine(_directory, id.ToString("N") + ".json");

    private async Task<QuarantineEntry> ReadMetadataAsync(Guid id, CancellationToken cancellationToken)
    {
        var path = MetadataPath(id);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Entrée de quarantaine introuvable.");
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<QuarantineEntry>(stream, JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException("Métadonnées de quarantaine vides.");
    }

    private async Task WriteMetadataAsync(QuarantineEntry entry, CancellationToken cancellationToken)
    {
        var final = MetadataPath(entry.Id);
        var temp = final + ".tmp";

        await using (var stream = File.Create(temp))
        {
            await JsonSerializer.SerializeAsync(stream, entry, JsonOptions, cancellationToken).ConfigureAwait(false);
        }

        File.Move(temp, final, overwrite: true); // écriture atomique
    }

    private static async Task MoveWithRetryAsync(string source, string destination, CancellationToken cancellationToken)
    {
        const int attempts = 5;
        for (var i = 1; ; i++)
        {
            try
            {
                File.Move(source, destination);
                return;
            }
            catch (IOException) when (i < attempts)
            {
                await Task.Delay(500, cancellationToken).ConfigureAwait(false); // fichier momentanément verrouillé
            }
        }
    }

    private static string UniqueTarget(string originalPath)
    {
        if (!File.Exists(originalPath))
        {
            return originalPath;
        }

        var directory = Path.GetDirectoryName(originalPath) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(originalPath);
        var extension = Path.GetExtension(originalPath);

        for (var i = 1; ; i++)
        {
            var candidate = Path.Combine(directory, $"{name} (restauré {i}){extension}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }
    }

    private static void EnsureDirectory(string directory, bool restrictAccess)
    {
        Directory.CreateDirectory(directory);
        if (!restrictAccess)
        {
            return;
        }

        var security = new DirectorySecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

        foreach (var sidType in new[] { WellKnownSidType.LocalSystemSid, WellKnownSidType.BuiltinAdministratorsSid })
        {
            security.AddAccessRule(new FileSystemAccessRule(
                new SecurityIdentifier(sidType, null),
                FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow));
        }

        new DirectoryInfo(directory).SetAccessControl(security);
    }
}
