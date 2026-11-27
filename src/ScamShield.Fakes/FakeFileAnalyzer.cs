using System.Security.Cryptography;
using System.Text;
using ScamShield.Core.Abstractions;
using ScamShield.Core.Models;

namespace ScamShield.Fakes;

/// <summary>
/// Analyseur factice : ne lit jamais le disque. Tout chemin contenant « eicar » est traité
/// comme une menace connue, ce qui permet de simuler le trajet complet sans vrai fichier dangereux.
/// </summary>
public sealed class FakeFileAnalyzer : IFileAnalyzer
{
    /// <inheritdoc />
    public Task<FileAnalysis> AnalyzeAsync(ScanRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.FilePath)));
        var isEicar = request.FilePath.Contains("eicar", StringComparison.OrdinalIgnoreCase);

        var reasons = new List<RiskReason>();
        if (isEicar)
        {
            reasons.Add(new RiskReason("TEST_SIGNATURE", "Fichier de test antivirus EICAR détecté.", 1.0));
        }

        var features = new Dictionary<string, float>
        {
            [FeatureNames.FileSize] = 1024f,
            [FeatureNames.Entropy] = isEicar ? 7.5f : 4.2f,
            [FeatureNames.SectionCount] = 4f,
            [FeatureNames.SuspiciousImportCount] = isEicar ? 6f : 0f,
            [FeatureNames.IsSigned] = isEicar ? 0f : 1f,
            [FeatureNames.HasPackedSection] = isEicar ? 1f : 0f,
        };

        var analysis = new FileAnalysis(
            request,
            sha256,
            SizeBytes: 1024,
            IsKnownThreat: isEicar,
            RuleScore: isEicar ? 100 : 10,
            RuleReasons: reasons,
            Features: new FeatureVector(FeatureVector.CurrentSchemaVersion, features));

        return Task.FromResult(analysis);
    }
}
