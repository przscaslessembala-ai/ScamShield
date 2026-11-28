namespace ScamShield.Agent;

/// <summary>Attend qu'un fichier soit entièrement écrit (taille stable et plus verrouillé en écriture).</summary>
public static class FileStabilizer
{
    /// <param name="gate">
    /// Limite le nombre d'ouvertures simultanées. Elle n'est tenue que pendant la tentative d'ouverture,
    /// jamais pendant l'attente entre deux essais : un fichier verrouillé ne bloque pas les autres.
    /// </param>
    /// <returns>Vrai si le fichier est prêt, faux s'il a disparu ou si le délai est dépassé.</returns>
    public static async Task<bool> WaitUntilReadyAsync(
        string path,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        TimeSpan? pollInterval = null,
        SemaphoreSlim? gate = null)
    {
        var interval = pollInterval ?? TimeSpan.FromMilliseconds(500);
        var started = DateTime.UtcNow;
        long lastSize = -1;

        while (DateTime.UtcNow - started < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (gate is not null)
            {
                await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

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
            finally
            {
                gate?.Release();
            }

            await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }
}