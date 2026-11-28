namespace ScamShield.Engine.Analysis;

/// <summary>Calcul de l'entropie de Shannon (0 à 8 bits par octet).</summary>
internal static class Entropy
{
    /// <summary>Calcule l'entropie à partir d'un histogramme de 256 compteurs.</summary>
    public static double FromHistogram(ReadOnlySpan<long> counts, long total)
    {
        if (total <= 0)
        {
            return 0;
        }

        double entropy = 0;
        foreach (var count in counts)
        {
            if (count == 0)
            {
                continue;
            }

            var p = (double)count / total;
            entropy -= p * Math.Log2(p);
        }

        return entropy;
    }
}
