using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScamShield.Core.Abstractions;
using ScamShield.Core.Models;

namespace ScamShield.Agent;

/// <summary>
/// Serveur named pipe pour l'interface (Joseph). Protocole : un message JSON par ligne, dans les deux sens.
/// Droits : SYSTEM et administrateurs (contrôle total), utilisateurs authentifiés (lecture/écriture), réseau refusé.
/// Chaque message est validé : taille limitée, JSON objet, type connu, paramètres contrôlés.
/// </summary>
public sealed class PipeServer(
    IOptions<AgentOptions> options,
    IQuarantineService quarantine,
    FileSubmitter submitter,
    ResultHub hub,
    ILogger<PipeServer> logger) : BackgroundService
{
    private const int MaxLineChars = 8192;
    private const int MaxClients = 10;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly SemaphoreSlim _clients = new(MaxClients);

    private sealed record Response(string Type, string? RequestId, bool Ok, object? Data, string? Error);

    private sealed record ScanEvent(string Type, ScanResult Result, QuarantineEntry? Quarantine);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Named pipe {Pipe} en écoute", options.Value.PipeName);

        while (!stoppingToken.IsCancellationRequested)
        {
            NamedPipeServerStream? server = null;
            try
            {
                server = CreateServer();
                await server.WaitForConnectionAsync(stoppingToken);

                if (!await _clients.WaitAsync(0, stoppingToken))
                {
                    logger.LogWarning("Trop de clients connectés au pipe : connexion refusée");
                    continue; // le finally libère le flux
                }

                var connected = server;
                server = null;
                _ = Task.Run(() => HandleClientAsync(connected, stoppingToken), CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erreur du serveur named pipe");
                await Task.Delay(1000, CancellationToken.None);
            }
            finally
            {
                server?.Dispose();
            }
        }
    }

    private NamedPipeServerStream CreateServer()
    {
        var security = new PipeSecurity();
        security.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), PipeAccessRights.FullControl, AccessControlType.Allow));
        security.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null), PipeAccessRights.FullControl, AccessControlType.Allow));
        security.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null), PipeAccessRights.ReadWrite, AccessControlType.Allow));
        security.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.NetworkSid, null), PipeAccessRights.FullControl, AccessControlType.Deny));

        return NamedPipeServerStreamAcl.Create(
            options.Value.PipeName,
            PipeDirection.InOut,
            NamedPipeServerStream.MaxAllowedServerInstances,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous,
            0,
            0,
            security);
    }

    private async Task HandleClientAsync(NamedPipeServerStream pipe, CancellationToken stoppingToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var token = linked.Token;
        var outbound = Channel.CreateBounded<string>(new BoundedChannelOptions(200)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
        });

        void OnOutcome(ScanOutcome outcome) =>
            outbound.Writer.TryWrite(JsonSerializer.Serialize(new ScanEvent("scanResult", outcome.Result, outcome.Quarantine), Json));

        hub.OutcomePublished += OnOutcome;
        Task? writerTask = null;

        try
        {
            await using (pipe)
            {
                var writer = new StreamWriter(pipe, new UTF8Encoding(false), 4096, leaveOpen: true) { AutoFlush = true };
                var reader = new StreamReader(pipe, Encoding.UTF8, false, 4096, leaveOpen: true);

                writerTask = Task.Run(async () =>
                {
                    await foreach (var line in outbound.Reader.ReadAllAsync(token))
                    {
                        await writer.WriteLineAsync(line.AsMemory(), token);
                    }
                }, CancellationToken.None);

                while (!token.IsCancellationRequested && pipe.IsConnected)
                {
                    var line = await ReadLineBoundedAsync(reader, token);
                    if (line is null)
                    {
                        break; // client déconnecté
                    }

                    if (line.Length == 0)
                    {
                        continue;
                    }

                    outbound.Writer.TryWrite(await HandleRequestAsync(line, token));
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Arrêt du service : normal.
        }
        catch (InvalidDataException ex)
        {
            logger.LogWarning("Client déconnecté : {Reason}", ex.Message);
        }
        catch (IOException)
        {
            // Client parti brutalement.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur avec un client du pipe");
        }
        finally
        {
            hub.OutcomePublished -= OnOutcome;
            outbound.Writer.TryComplete();
            linked.Cancel();

            if (writerTask is not null)
            {
                try
                {
                    await writerTask;
                }
                catch (Exception)
                {
                    // Écriture interrompue par la déconnexion : sans importance.
                }
            }

            _clients.Release();
        }
    }

    private static async Task<string?> ReadLineBoundedAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        var buffer = new char[1];

        while (true)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(), cancellationToken);
            if (read == 0)
            {
                return builder.Length == 0 ? null : builder.ToString();
            }

            if (buffer[0] == '\n')
            {
                return builder.ToString().TrimEnd('\r');
            }

            if (builder.Length >= MaxLineChars)
            {
                throw new InvalidDataException("Message trop long.");
            }

            builder.Append(buffer[0]);
        }
    }

    private async Task<string> HandleRequestAsync(string line, CancellationToken cancellationToken)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(line);
        }
        catch (JsonException)
        {
            return Reply(null, false, null, "JSON invalide.");
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return Reply(null, false, null, "Le message doit être un objet JSON.");
            }

            var requestId = GetString(root, "requestId", 64);
            var type = GetString(root, "type", 32);

            try
            {
                switch (type)
                {
                    case "ping":
                        return Reply(requestId, true, "pong", null);

                    case "listQuarantine":
                        return Reply(requestId, true, await quarantine.ListAsync(cancellationToken), null);

                    case "restore" when TryGetGuid(root, out var restoreId):
                        await quarantine.RestoreAsync(restoreId, cancellationToken);
                        return Reply(requestId, true, null, null);

                    case "delete" when TryGetGuid(root, out var deleteId):
                        await quarantine.DeleteAsync(deleteId, cancellationToken);
                        return Reply(requestId, true, null, null);

                    case "restore" or "delete":
                        return Reply(requestId, false, null, "Identifiant invalide.");

                    case "scanFile":
                        var path = GetString(root, "path", 1024);
                        if (path is null || !Path.IsPathFullyQualified(path) || path.StartsWith(@"\\", StringComparison.Ordinal) || !File.Exists(path))
                        {
                            return Reply(requestId, false, null, "Chemin invalide.");
                        }

                        submitter.Fire(path, ScanTrigger.Manual);
                        return Reply(requestId, true, "queued", null);

                    default:
                        return Reply(requestId, false, null, "Type de message inconnu.");
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (FileNotFoundException)
            {
                return Reply(requestId, false, null, "Élément introuvable.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Échec de la commande {Type}", type);
                return Reply(requestId, false, null, "Opération impossible.");
            }
        }
    }

    private static string Reply(string? requestId, bool ok, object? data, string? error) =>
        JsonSerializer.Serialize(new Response("response", requestId, ok, data, error), Json);

    private static string? GetString(JsonElement root, string name, int maxLength) =>
        root.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
        && value.GetString() is { Length: > 0 } text
        && text.Length <= maxLength
            ? text
            : null;

    private static bool TryGetGuid(JsonElement root, out Guid id)
    {
        id = Guid.Empty;
        return GetString(root, "id", 64) is { } text && Guid.TryParse(text, out id);
    }
}
