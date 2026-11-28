using PeNet;
using PeNet.Header.Authenticode;

namespace ScamShield.Engine.Analysis;

/// <summary>Mesures extraites de la structure d'un exécutable Windows (PE).</summary>
internal sealed record PeMeasures(
    bool IsPe,
    int SectionCount,
    int SuspiciousImportCount,
    bool IsSigned,
    bool HasPackedSection)
{
    public static PeMeasures NotPe { get; } = new(false, 0, 0, false, false);
}

/// <summary>Analyse de structure PE avec PeNet. Ne lève jamais d'exception : un fichier illisible est « non PE ».</summary>
internal static class PeInspector
{
    private const long MaxPeBytes = 200L * 1024 * 1024;
    private const long MaxSectionBytes = 32L * 1024 * 1024;
    private const double PackedSectionEntropy = 7.2;

    private static readonly HashSet<string> SuspiciousApis = new(StringComparer.OrdinalIgnoreCase)
    {
        "VirtualAlloc", "VirtualAllocEx", "VirtualProtect", "WriteProcessMemory", "ReadProcessMemory",
        "CreateRemoteThread", "NtUnmapViewOfSection", "SetWindowsHookEx", "GetAsyncKeyState",
        "IsDebuggerPresent", "CheckRemoteDebuggerPresent", "URLDownloadToFile", "WinExec",
        "AdjustTokenPrivileges", "OpenProcess", "ShellExecute",
    };

    public static PeMeasures Inspect(string path)
    {
        try
        {
            var length = new FileInfo(path).Length;
            if (length < 64 || length > MaxPeBytes)
            {
                return PeMeasures.NotPe;
            }

            if (!PeFile.TryParse(path, out var pe) || pe is null)
            {
                return PeMeasures.NotPe;
            }

            var sections = pe.ImageSectionHeaders ?? [];
            var imports = pe.ImportedFunctions ?? [];

            var suspicious = imports
                .Select(f => f.Name)
                .Where(n => !string.IsNullOrEmpty(n))
                .Select(n => n!)
                .Where(IsSuspiciousApi)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            var packed = false;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                foreach (var section in sections)
                {
                    if (section.SizeOfRawData < 1024 || section.Name == ".rsrc")
                    {
                        continue;
                    }

                    if (SectionEntropy(stream, section.PointerToRawData, section.SizeOfRawData) >= PackedSectionEntropy)
                    {
                        packed = true;
                        break;
                    }
                }
            }

            var signed = HasValidSignature(pe);

            return new PeMeasures(true, sections.Length, suspicious, signed, packed);
        }
        catch (Exception)
        {
            return PeMeasures.NotPe;
        }
    }
    private static bool HasValidSignature(PeFile pe)
{
    try
    {
        return new AuthenticodeInfo(pe).IsAuthenticodeValid;
    }
    catch (Exception)
    {
        return false; // fichier non signé ou signature illisible : traité comme non signé
    }
}

    private static bool IsSuspiciousApi(string name)
    {
        if (SuspiciousApis.Contains(name))
        {
            return true;
        }

        // Variantes ANSI / Unicode : ShellExecuteA, ShellExecuteW...
        return name.Length > 1
            && (name[^1] == 'A' || name[^1] == 'W')
            && SuspiciousApis.Contains(name[..^1]);
    }

    private static double SectionEntropy(FileStream stream, long offset, long length)
    {
        if (length <= 0 || offset < 0 || offset >= stream.Length)
        {
            return 0;
        }

        length = Math.Min(Math.Min(length, stream.Length - offset), MaxSectionBytes);
        stream.Seek(offset, SeekOrigin.Begin);

        var counts = new long[256];
        var buffer = new byte[81920];
        var remaining = length;

        while (remaining > 0)
        {
            var read = stream.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
            if (read == 0)
            {
                break;
            }

            for (var i = 0; i < read; i++)
            {
                counts[buffer[i]]++;
            }

            remaining -= read;
        }

        return Entropy.FromHistogram(counts, length - remaining);
    }
}
