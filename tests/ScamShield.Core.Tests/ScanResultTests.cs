using ScamShield.Core.Models;
using Xunit;

namespace ScamShield.Core.Tests;

public sealed class ScanResultTests
{
    private static FileAnalysis CreateAnalysis(bool isKnownThreat)
    {
        var request = new ScanRequest(@"C:\Users\test\Downloads\sample.exe", ScanTrigger.Download);
        var features = new FeatureVector(FeatureVector.CurrentSchemaVersion, new Dictionary<string, float>());

        return new FileAnalysis(request, new string('a', 64), 2048, isKnownThreat, 0, [], features);
    }

    [Fact]
    public void KnownThreatIsAlwaysDangerous()
    {
        var analysis = CreateAnalysis(isKnownThreat: true);
        var assessment = new RiskAssessment(0.1, []);

        var result = ScanResult.Create(analysis, assessment);

        Assert.Equal(Verdict.Dangerous, result.Verdict);
        Assert.Equal(1.0, result.Score);
        Assert.Contains(result.Reasons, reason => reason.Code == RiskReasonCodes.KnownThreat);
    }

    [Theory]
    [InlineData(0.2, Verdict.Safe)]
    [InlineData(0.6, Verdict.Suspicious)]
    [InlineData(0.9, Verdict.Dangerous)]
    public void UnknownFileFollowsPolicy(double score, Verdict expected)
    {
        var analysis = CreateAnalysis(isKnownThreat: false);
        var assessment = new RiskAssessment(score, []);

        var result = ScanResult.Create(analysis, assessment);

        Assert.Equal(expected, result.Verdict);
        Assert.Equal(score, result.Score);
    }

    [Fact]
    public void ModelReasonsAreKept()
    {
        var analysis = CreateAnalysis(isKnownThreat: false);
        var reason = new RiskReason("HIGH_ENTROPY", "Contenu compressé ou chiffré.", 0.4);
        var assessment = new RiskAssessment(0.6, [reason]);

        var result = ScanResult.Create(analysis, assessment);

        Assert.Single(result.Reasons);
        Assert.Equal(reason, result.Reasons[0]);
    }

    [Fact]
    public void ResultKeepsRequestIdentity()
    {
        var analysis = CreateAnalysis(isKnownThreat: false);

        var result = ScanResult.Create(analysis, new RiskAssessment(0.1, []));

        Assert.Equal(analysis.Request.Id, result.RequestId);
        Assert.Equal(analysis.Request.FilePath, result.FilePath);
        Assert.Equal(analysis.Sha256, result.Sha256);
    }

    [Fact]
    public void ScanRequestRejectsEmptyPath()
    {
        Assert.ThrowsAny<ArgumentException>(() => new ScanRequest("  ", ScanTrigger.Manual));
    }
}
