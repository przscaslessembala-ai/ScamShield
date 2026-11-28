using ScamShield.Core.Abstractions;
using ScamShield.Core.Models;

namespace ScamShield.Engine.Analysis;

/// <summary>
/// Analyse classique d'un fichier : hash, comparaison à la base des menaces, structure PE, règles,
/// puis vecteur de caractéristiques pour l'IA.
/// </summary>
public sealed class FileAnalyzer(IThreatRepository threats) : IFileAnalyzer
{
    /// <inheritdoc />
    public async Task<FileAnalysis> AnalyzeAsync(ScanRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var digest = await FileDigester.ComputeAsync(request.FilePath, cancellationToken).ConfigureAwait(false);
        var threat = await threats.FindBySha256Async(digest.Sha256, cancellationToken).ConfigureAwait(false);
        var pe = await Task.Run(() => PeInspector.Inspect(request.FilePath), cancellationToken).ConfigureAwait(false);
        var rules = RuleEngine.Evaluate(request.FilePath, digest.Entropy, pe);

        var values = new Dictionary<string, float>
        {
            [FeatureNames.FileSize] = digest.SizeBytes,
            [FeatureNames.Entropy] = (float)digest.Entropy,
            [FeatureNames.SectionCount] = pe.SectionCount,
            [FeatureNames.SuspiciousImportCount] = pe.SuspiciousImportCount,
            [FeatureNames.IsSigned] = pe.IsSigned ? 1f : 0f,
            [FeatureNames.HasPackedSection] = pe.HasPackedSection ? 1f : 0f,
        };

        var features = new FeatureVector(FeatureVector.CurrentSchemaVersion, values);

        return new FileAnalysis(
            request,
            digest.Sha256,
            digest.SizeBytes,
            threat is not null,
            rules.Score,
            rules.Reasons,
            features);
    }
}
