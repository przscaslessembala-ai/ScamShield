using ScamShield.Core.Models;

namespace ScamShield.Core.Abstractions;

/// <summary>
/// Analyse classique d'un fichier : hash, structure, règles.
/// Implémentée par ScamShield.Engine.
/// </summary>
public interface IFileAnalyzer
{
    /// <summary>Analyse le fichier désigné par la demande.</summary>
    /// <param name="request">Demande d'analyse.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    Task<FileAnalysis> AnalyzeAsync(ScanRequest request, CancellationToken cancellationToken = default);
}
