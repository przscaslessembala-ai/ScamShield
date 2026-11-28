using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using ScamShield.Core.Models;
using ScamShield.Engine.Analysis;
using ScamShield.Engine.Scoring;
using ScamShield.Engine.Storage;
using Xunit;

namespace ScamShield.Engine.Tests;

public sealed class EngineTests : IDisposable
{
    // Le fichier EICAR n'est volontairement jamais écrit sur disque ici (l'antivirus de la machine le supprimerait).
    // La chaîne est coupée en deux pour ne pas déclencher d'alerte à l'enregistrement du fichier source.
    private const string EicarPart1 = "X5O!P%@AP[4\\PZX54(P^)7CC)7}$";
    private const string EicarPart2 = "EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*";

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "scamshield-tests-" + Guid.NewGuid().ToString("N"));

    public EngineTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(_dir, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    private SqliteThreatRepository NewRepository() => new(Path.Combine(_dir, "threats.db"));

    private string WriteFile(string name, byte[] content)
    {
        var path = Path.Combine(_dir, name);
        File.WriteAllBytes(path, content);
        return path;
    }

    [Fact]
    public async Task Repository_Knows_Eicar_Hash_Out_Of_The_Box()
    {
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.ASCII.GetBytes(EicarPart1 + EicarPart2)));

        var found = await NewRepository().FindBySha256Async(hash);

        Assert.Equal(KnownHashes.EicarSha256, hash);
        Assert.NotNull(found);
        Assert.Equal("EICAR-Test-File", found!.Name);
    }

    [Fact]
    public async Task Repository_Returns_Null_For_Unknown_Or_Invalid_Hash()
    {
        var repository = NewRepository();

        Assert.Null(await repository.FindBySha256Async(new string('a', 64)));
        Assert.Null(await repository.FindBySha256Async("pas-un-hash"));
    }

    [Fact]
    public async Task Repository_Add_Then_Find_Works_And_Is_Case_Insensitive()
    {
        var repository = NewRepository();
        var sha = new string('B', 64);

        await repository.AddAsync(new ThreatRecord(sha, "Test", "unit", DateTimeOffset.UtcNow));
        var found = await repository.FindBySha256Async(sha.ToLowerInvariant());

        Assert.NotNull(found);
        Assert.Equal("Test", found!.Name);
    }

    [Fact]
    public async Task Analyzer_Computes_Known_Sha256_And_Fills_All_Features()
    {
        var path = WriteFile("hello.txt", Encoding.ASCII.GetBytes("hello"));
        var analyzer = new FileAnalyzer(NewRepository());

        var analysis = await analyzer.AnalyzeAsync(new ScanRequest(path, ScanTrigger.Manual));

        Assert.Equal("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824", analysis.Sha256);
        Assert.Equal(5, analysis.SizeBytes);
        Assert.False(analysis.IsKnownThreat);
        Assert.Equal(FeatureVector.CurrentSchemaVersion, analysis.Features.SchemaVersion);
        foreach (var name in FeatureNames.All)
        {
            Assert.True(analysis.Features.Values.ContainsKey(name), $"Caractéristique manquante : {name}");
        }

        Assert.Equal(5f, analysis.Features.Values[FeatureNames.FileSize]);
    }

    [Fact]
    public async Task Analyzer_Flags_Known_Threat_By_Hash()
    {
        var path = WriteFile("menace.bin", Encoding.ASCII.GetBytes("hello"));
        var repository = NewRepository();
        await repository.AddAsync(new ThreatRecord(
            "2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824", "Menace de test", "unit", DateTimeOffset.UtcNow));

        var analysis = await new FileAnalyzer(repository).AnalyzeAsync(new ScanRequest(path, ScanTrigger.Download));
        var assessment = await new RuleBasedRiskScorer().ScoreAsync(analysis);
        var result = ScanResult.Create(analysis, assessment);

        Assert.True(analysis.IsKnownThreat);
        Assert.Equal(Verdict.Dangerous, result.Verdict);
    }

    [Fact]
    public async Task Analyzer_Reads_Pe_Structure_Of_A_Real_Executable()
    {
        // Une DLL .NET est un fichier PE : on utilise celle des tests.
        var path = typeof(EngineTests).Assembly.Location;

        var analysis = await new FileAnalyzer(NewRepository()).AnalyzeAsync(new ScanRequest(path, ScanTrigger.Manual));

        Assert.True(analysis.Features.Values[FeatureNames.SectionCount] > 0);
    }

    [Fact]
    public void Rules_Score_Stacked_Indicators_Below_Dangerous_Threshold()
    {
        var pe = new PeMeasures(true, 5, 4, IsSigned: false, HasPackedSection: true);

        var outcome = RuleEngine.Evaluate(@"C:\Users\x\Downloads\setup.exe", 7.6, pe);

        Assert.Equal(75, outcome.Score);
        Assert.Contains(outcome.Reasons, r => r.Code == EngineReasonCodes.HighEntropy);
        Assert.Contains(outcome.Reasons, r => r.Code == EngineReasonCodes.PackedSection);
        Assert.Contains(outcome.Reasons, r => r.Code == EngineReasonCodes.SuspiciousImports);
        Assert.Contains(outcome.Reasons, r => r.Code == EngineReasonCodes.UnsignedExecutable);
    }

    [Fact]
    public void Rules_Do_Not_Flag_A_Signed_Plain_Executable()
    {
        var pe = new PeMeasures(true, 5, 0, IsSigned: true, HasPackedSection: false);

        var outcome = RuleEngine.Evaluate(@"C:\Program Files\app\app.exe", 5.5, pe);

        Assert.Equal(0, outcome.Score);
        Assert.Empty(outcome.Reasons);
    }

    [Theory]
    [InlineData(@"E:\facture.pdf.exe", EngineReasonCodes.DoubleExtension)]
    [InlineData(@"E:\autorun.inf", EngineReasonCodes.AutorunFile)]
    [InlineData(@"E:\ecran.scr", EngineReasonCodes.ScreensaverExecutable)]
    public void Rules_Detect_Suspicious_File_Names(string path, string expectedCode)
    {
        var outcome = RuleEngine.Evaluate(path, 0, PeMeasures.NotPe);

        Assert.Contains(outcome.Reasons, r => r.Code == expectedCode);
    }

    [Fact]
    public async Task Fallback_Scorer_Returns_Score_Between_0_And_1()
    {
        var path = WriteFile("a.txt", Encoding.ASCII.GetBytes("bonjour"));
        var analysis = await new FileAnalyzer(NewRepository()).AnalyzeAsync(new ScanRequest(path, ScanTrigger.Manual));

        var assessment = await new RuleBasedRiskScorer().ScoreAsync(analysis);

        Assert.InRange(assessment.Score, 0.0, 1.0);
    }
}
