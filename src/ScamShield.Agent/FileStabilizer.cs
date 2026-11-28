namespace ScamShield.Agent;

/// <summary>Attend qu'un fichier soit entièrement écrit (taille stable et plus verrouillé en écriture).</summary>
public static class FileStabilizer
{
    /// <returns>Vrai si le fichier est prêt, faux s'il a disparu ou si le délai est dépassé.</returns>
    public static async Task<bool> WaitUntilReadyAsync(
        string path,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        TimeSpan? pollInterval = null)
    {
        var interval = pollInterval ?? TimeSpan.FromMilliseconds(500);
        var started = DateTime.UtcNow;
        long lastSize = -1;

        while (DateTime.UtcNow - started < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // FileShare.Read échoue tant qu'un autre processus a le fichier ouvert en écriture.
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                var size = stream.Length;
                if (size == lastSize)
                {
                    return true;
                }

                lastSize = size;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            catch (DirectoryNotFoundException)
            {
                return false;
            }
            catch (IOException)
            {
                lastSize = -1; // verrouillé : on réessaie
            }
            catch (UnauthorizedAccessException)
            {
                lastSize = -1; // accès refusé (peut-être temporaire) : on réessaie jusqu'au délai
            }

            await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }
}
