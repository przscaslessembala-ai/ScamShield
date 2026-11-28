using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScamShield.Core.Abstractions;
using ScamShield.Core.Models;
using ScamShield.Engine.Scoring;

namespace ScamShield.Agent;

/// <summary>
/// Étapes 2 à 4 du trajet : analyse (Engine), score (IA ou règles), décision, quarantaine.
/// Un fichier en erreur est journalisé puis ignoré : le service ne s'arrête jamais pour un fichier.
/// </summary>
public sealed class ScanWorker(
    ScanQueue queue,
    IFileAnalyzer analyzer,
    IRiskScorer scorer,
    IQuarantineService quarantine,
    UserAllowList allowList,
    ResultHub hub,
    ILogger<ScanWorker> logger) : BackgroundService
{
    private readonly IRiskScorer _fallbackScorer = new RuleBasedRiskScorer();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var request in queue.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await ProcessAsync(request, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
                {
                    logger.LogWarning("Fichier disparu avant l'analyse : {Path}", request.FilePath);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Échec de l'analyse de {Path}", request.FilePath);
                }
                finally
                {
                    queue.Release(request.FilePath);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Arrêt du service : normal.
        }
    }

    private async Task ProcessAsync(ScanRequest request, CancellationToken cancellationToken)
    {
        var analysis = await analyzer.AnalyzeAsync(request, cancellationToken);

        RiskAssessment assessment;
        try
        {
            assessment = await scorer.ScoreAsync(analysis, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Le scorer principal a échoué, bascule sur le scorer à base de règles");
            assessment = await _fallbackScorer.ScoreAsync(analysis, cancellationToken);
        }

        var result = ScanResult.Create(analysis, assessment);
        QuarantineEntry? entry = null;

        if (result.Verdict == Verdict.Dangerous)
        {
            if (allowList.IsAllowed(result.Sha256))
            {
                logger.LogInformation("Fichier dangereux déjà restauré par l'utilisateur, laissé en place : {Path}", result.FilePath);
            }
            else
            {
                entry = await quarantine.QuarantineAsync(result, cancellationToken);
            }
        }

        logger.LogInformation(
            "Analyse terminée : {Path} → {Verdict} (score {Score:0.00}, {Count} raison(s))",
            result.FilePath,
            result.Verdict,
            result.Score,
            result.Reasons.Count);

        hub.Publish(new ScanOutcome(result, entry));
    }
}
