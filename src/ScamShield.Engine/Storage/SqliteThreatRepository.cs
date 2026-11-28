using System.Globalization;
using Microsoft.Data.Sqlite;
using ScamShield.Core.Abstractions;
using ScamShield.Core.Models;

namespace ScamShield.Engine.Storage;

/// <summary>Hashs de référence préchargés dans la base.</summary>
public static class KnownHashes
{
    /// <summary>SHA-256 du fichier de test EICAR (68 octets, sans retour à la ligne).</summary>
    public const string EicarSha256 = "275a021bbfb6489e54d471899f7db9d1663fc695ec2fe2a2c4538aabf651fd0f";
}

/// <summary>Base SQLite des menaces connues. EICAR est préchargé à la création.</summary>
public sealed class SqliteThreatRepository : IThreatRepository
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    /// <summary>Crée le dépôt ; le fichier et le dossier sont créés si nécessaire.</summary>
    /// <param name="databasePath">Chemin du fichier SQLite.</param>
    public SqliteThreatRepository(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        var directory = Path.GetDirectoryName(Path.GetFullPath(databasePath));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false,
        }.ToString();
    }

    /// <inheritdoc />
    public async Task<ThreatRecord?> FindBySha256Async(string sha256, CancellationToken cancellationToken = default)
    {
        if (!IsValidSha256(sha256))
        {
            return null;
        }

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT sha256, name, source, added_at FROM threats WHERE sha256 = $sha;";
        command.Parameters.AddWithValue("$sha", sha256.ToLowerInvariant());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return new ThreatRecord(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            DateTimeOffset.Parse(reader.GetString(3), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
    }

    /// <inheritdoc />
    public async Task AddAsync(ThreatRecord threat, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(threat);

        if (!IsValidSha256(threat.Sha256))
        {
            throw new ArgumentException("Le hash doit être un SHA-256 de 64 caractères hexadécimaux.", nameof(threat));
        }

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO threats (sha256, name, source, added_at)
            VALUES ($sha, $name, $source, $added)
            ON CONFLICT(sha256) DO UPDATE SET name = $name, source = $source, added_at = $added;
            """;
        command.Parameters.AddWithValue("$sha", threat.Sha256.ToLowerInvariant());
        command.Parameters.AddWithValue("$name", threat.Name);
        command.Parameters.AddWithValue("$source", threat.Source);
        command.Parameters.AddWithValue("$added", threat.AddedAt.ToString("O", CultureInfo.InvariantCulture));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static bool IsValidSha256(string? value) =>
        value is { Length: 64 } && value.All(Uri.IsHexDigit);

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
        {
            return;
        }

        await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized)
            {
                return;
            }

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS threats (
                    sha256   TEXT PRIMARY KEY,
                    name     TEXT NOT NULL,
                    source   TEXT NOT NULL,
                    added_at TEXT NOT NULL
                );
                INSERT OR IGNORE INTO threats (sha256, name, source, added_at)
                VALUES ($eicar, 'EICAR-Test-File', 'Fichier de test standard EICAR', $now);
                """;
            command.Parameters.AddWithValue("$eicar", KnownHashes.EicarSha256);
            command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }
}
