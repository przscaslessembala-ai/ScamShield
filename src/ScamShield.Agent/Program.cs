using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Serilog;
using ScamShield.Agent;
using ScamShield.Core.Abstractions;
using ScamShield.Engine;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(o => o.ServiceName = "ScamShield Agent");
builder.Services.Configure<AgentOptions>(builder.Configuration.GetSection("Agent"));

var agentOptions = builder.Configuration.GetSection("Agent").Get<AgentOptions>() ?? new AgentOptions();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(AgentPaths.Logs(agentOptions), "agent-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger, dispose: true);

// Priorité basse : l'antivirus ne doit jamais ralentir le PC.
try
{
    using var process = Process.GetCurrentProcess();
    process.PriorityClass = ProcessPriorityClass.BelowNormal;
}
catch (Exception ex)
{
    Log.Warning(ex, "Impossible de baisser la priorité du processus");
}

// Engine (analyseur, base SQLite, scorer de secours). Le module IA pourra enregistrer son IRiskScorer avant cet appel.
builder.Services.AddScamShieldEngine(AgentPaths.ThreatDatabase(agentOptions));

builder.Services.AddSingleton<UserAllowList>();
builder.Services.AddSingleton<ResultHub>();
builder.Services.AddSingleton<ScanQueue>();
builder.Services.AddSingleton<FileSubmitter>();
builder.Services.AddSingleton<IQuarantineService, QuarantineService>();

builder.Services.AddHostedService<ScanWorker>();
builder.Services.AddHostedService<DownloadsMonitor>();
builder.Services.AddHostedService<UsbMonitor>();
builder.Services.AddHostedService<PipeServer>();

try
{
    Log.Information("ScamShield Agent démarre (données : {Directory})", agentOptions.DataDirectory);
    await builder.Build().RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "L'Agent s'est arrêté de façon inattendue");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
