using ScamShield.Core.Abstractions;
using ScamShield.Core.Models;
using ScamShield.Fakes;
using Xunit;

namespace ScamShield.Core.Tests;

/// <summary>
/// Trajet complet avec les implémentations factices : détection, analyse, score, verdict, quarantaine.
/// Ce test sert de modèle pour les futurs tests d'intégration avec les vrais modules.
/// </summary>
public sealed class FakePipelineTests
{
    private readonly IFileAnalyzer _analyzer = new FakeFileAnalyzer();
    private readonly IRiskScorer _scorer = new FakeRiskScorer();
    private readonly IQuarantineService _quarantine = new InMemoryQuarantineService();

    private async Task<ScanResult> ScanAsync(string path)
    {
        var request = new ScanRequest(path, ScanTrigger.Download);
        var analysis = await _analyzer.AnalyzeAsync(request);
        var assessment = await _scorer.ScoreAsync(analysis);

        return ScanResult.Create(analysis, assessment);
    }

    [Fact]
    public async Task EicarFileEndsInQuarantineAndCanBeRestored()
    {
        var result = await ScanAsync(@"C:\Users\test\Downloads\eicar.com");

        Assert.Equal(Verdict.Dangerous, result.Verdict);

        var entry = await _quarantine.QuarantineAsync(result);
        Assert.Single(await _quarantine.ListAsync());

        await _quarantine.RestoreAsync(entry.Id);
        Assert.Empty(await _quarantine.ListAsync());
    }

    [Fact]
    public async Task HarmlessFileIsNotFlagged()
    {
        var result = await ScanAsync(@"C:\Users\test\Downloads\rapport.pdf");

        Assert.Equal(Verdict.Safe, result.Verdict);
    }

    [Fact]
    public async Task RestoringUnknownEntryFails()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _quarantine.RestoreAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task KnownThreatRepositoryFindsRegisteredHash()
    {
        var repository = new InMemoryThreatRepository();
        var threat = new ThreatRecord("ABC123", "Test-Threat", "test", DateTimeOffset.UtcNow);

        await repository.AddAsync(threat);

        Assert.Equal(threat, await repository.FindBySha256Async("abc123"));
        Assert.Null(await repository.FindBySha256Async("unknown"));
    }
}
