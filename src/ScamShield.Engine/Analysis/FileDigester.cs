using System.Security.Cryptography;

namespace ScamShield.Engine.Analysis;

/// <summary>Résultat de la lecture complète d'un fichier.</summary>
internal sealed record FileDigest(string Sha256, long SizeBytes, double Entropy);

/// <summary>Calcule SHA-256, taille et entropie en une seule lecture, sans charger le fichier en mémoire.</summary>
internal static class FileDigester
{
    private const int BufferSize = 81920;

    public static async Task<FileDigest> ComputeAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            BufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var counts = new long[256];
        var buffer = new byte[BufferSize];
        long total = 0;
        int read;

        while ((read = await stream.ReadAsync(buffer.AsMemory(0, BufferSize), cancellationToken)) > 0)
        {
            sha.AppendData(buffer, 0, read);
            for (var i = 0; i < read; i++)
            {
                counts[buffer[i]]++;
            }

            total += read;
        }

        var hash = Convert.ToHexStringLower(sha.GetHashAndReset());
        return new FileDigest(hash, total, Entropy.FromHistogram(counts, total));
    }
}
