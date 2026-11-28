using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ScamShield.Core.Abstractions;
using ScamShield.Engine.Analysis;
using ScamShield.Engine.Scoring;
using ScamShield.Engine.Storage;

namespace ScamShield.Engine;

/// <summary>Enregistrement des services de l'Engine dans l'injection de dépendances.</summary>
public static class EngineServiceCollectionExtensions
{
    /// <summary>
    /// Ajoute IThreatRepository (SQLite), IFileAnalyzer et le scorer de secours à base de règles.
    /// TryAdd : si le module IA a déjà enregistré son IRiskScorer, il est conservé.
    /// </summary>
    public static IServiceCollection AddScamShieldEngine(this IServiceCollection services, string databasePath)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        services.TryAddSingleton<IThreatRepository>(_ => new SqliteThreatRepository(databasePath));
        services.TryAddSingleton<IFileAnalyzer, FileAnalyzer>();
        services.TryAddSingleton<IRiskScorer, RuleBasedRiskScorer>();
        return services;
    }
}
