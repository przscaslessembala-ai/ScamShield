using System.Security.Cryptography;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ScamShield.Core.Models;
using Xunit;

namespace ScamShield.Agent.Tests;

public sealed class AgentTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "scamshield-agent-tests-" + Guid.NewGuid().ToString("N"));

    public AgentTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try
        {
            Directory.Delete(_dir, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    private QuarantineService NewQuarantine() =>
        new(Options.Create(new AgentOptions { DataDirectory = Path.Combine(_dir, "data") }),
            new UserAllowList(),
            NullLogger<QuarantineService>.Instance);

    private (string Path, byte[] Content, ScanResult Result) NewDangerousFile(string name)
    {
        var content = RandomNumberGenerator.GetBytes(4096);
        var path = Path.Combine(_dir, name);
        File.WriteAllBytes(path, content);

        var result = new ScanResult(
            Guid.NewGuid(), path, Convert.ToHexStringLower(SHA256.HashData(content)),
            Verdict.Dangerous, 0.95, [new RiskReason("TEST", "Raison de test", 0.9)], DateTimeOffset.UtcNow);

        return (path, content, result);
    }

    [Fact]
    public async Task Quarantine_Then_Restore_Gives_Back_The_Identical_File()
    {
        var service = NewQuarantine();
        var (path, content, result) = NewDangerousFile("virus.exe");

        var entry = await service.QuarantineAsync(result);

        Assert.False(File.Exists(path));
        Assert.True(File.Exists(entry.StoredPath));
        Assert.Equal(".quarantine", Path.GetExtension(entry.StoredPath));
        Assert.Single(await service.ListAsync());

        await service.RestoreAsync(entry.Id);

        Assert.True(File.Exists(path));
        Assert.Equal(content, await File.ReadAllBytesAsync(path));
        Assert.Empty(await service.ListAsync());
    }

    [Fact]
    public async Task Restore_Does_Not_Overwrite_A_File_That_Reappeared()
    {
        var service = NewQuarantine();
        var (path, _, result) = NewDangerousFile("doc.exe");
        var entry = await service.QuarantineAsync(result);
        await File.WriteAllTextAsync(path, "autre fichier");

        await service.RestoreAsync(entry.Id);

        Assert.Equal("autre fichier", await File.ReadAllTextAsync(path));
        Assert.Single(Directory.GetFiles(_dir, "doc (restauré 1).exe"));
    }

    [Fact]
    public async Task Delete_Removes_The_File_Permanently()
    {
        var service = NewQuarantine();
        var (_, _, result) = NewDangerousFile("malware.bin");
        var entry = await service.QuarantineAsync(result);

        await service.DeleteAsync(entry.Id);

        Assert.False(File.Exists(entry.StoredPath));
        Assert.Empty(await service.ListAsync());
    }

    [Fact]
    public async Task Restore_Of_Unknown_Entry_Throws()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(() => NewQuarantine().RestoreAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Stabilizer_Waits_For_A_Locked_File_To_Be_Released()
    {
        var path = Path.Combine(_dir, "download.bin");
        var writer = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);

        var whileLocked = await FileStabilizer.WaitUntilReadyAsync(
            path, TimeSpan.FromSeconds(1), CancellationToken.None, TimeSpan.FromMilliseconds(100));
        writer.Dispose();
        var afterRelease = await FileStabilizer.WaitUntilReadyAsync(
            path, TimeSpan.FromSeconds(5), CancellationToken.None, TimeSpan.FromMilliseconds(100));

        Assert.False(whileLocked);
        Assert.True(afterRelease);
    }

    [Fact]
    public async Task Stabilizer_Returns_False_For_A_Missing_File()
    {
        var ready = await FileStabilizer.WaitUntilReadyAsync(
            Path.Combine(_dir, "absent.bin"), TimeSpan.FromSeconds(2), CancellationToken.None, TimeSpan.FromMilliseconds(100));

        Assert.False(ready);
    }
}
