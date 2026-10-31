using System.Net.Sockets;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Sumlers.Api.Data;
using Sumlers.Api.Settings;

namespace Sumlers.Api.Health;

/// <summary>
/// GET /health for an outside uptime monitor (for example UptimeRobot). It answers 503 when content cannot be saved
/// or the mail server cannot be reached. The site cannot email anyone when it is down, so the monitor does that.
/// </summary>
public static class HealthEndpoints
{
    public static IServiceCollection AddSumlersHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<StorageHealthCheck>("storage")
            .AddCheck<EmailHealthCheck>("email");
        return services;
    }

    public static void MapSumlersHealthChecks(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = WriteResponseAsync });
    }

    // Status words only, never error details, because this page is public.
    private static Task WriteResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var body = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.ToDictionary(e => e.Key, e => new { status = e.Value.Status.ToString(), e.Value.Description }),
        };
        return context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonSerializerOptions.Web));
    }
}

/// <summary>Checks that App_Data accepts writes, since every admin save and contact message goes there.</summary>
public sealed class StorageHealthCheck(AppDataFolder dataFolder) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            Directory.CreateDirectory(dataFolder.Root);
            var probe = dataFolder.PathFor(".health-probe");
            await File.WriteAllTextAsync(probe, DateTime.UtcNow.ToString("O"), cancellationToken);
            File.Delete(probe);
            return HealthCheckResult.Healthy("Content can be saved.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Content cannot be saved.", ex);
        }
    }
}

/// <summary>
/// Checks that the mail server accepts connections. Without a mail server it reports Degraded (still HTTP 200):
/// messages are saved but nobody is emailed. Results are cached for 5 minutes so monitors do not hammer the server.
/// </summary>
public sealed class EmailHealthCheck(ISettingsStore settingsStore, TimeProvider clock) : IHealthCheck
{
    private static readonly TimeSpan _CacheFor = TimeSpan.FromMinutes(5);
    private (DateTimeOffset At, string Server, HealthCheckResult Result)? _Last;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var email = (await settingsStore.GetAsync(cancellationToken)).Email;
        if (!email.UsesSmtp) return HealthCheckResult.Degraded("No mail server is set. Messages are saved but not emailed.");

        var server = $"{email.Host}:{email.Port}";
        if (_Last is { } last && last.Server == server && clock.GetUtcNow() - last.At < _CacheFor) return last.Result;

        var result = await ConnectAsync(email.Host, email.Port, cancellationToken);
        _Last = (clock.GetUtcNow(), server, result);
        return result;
    }

    private static async Task<HealthCheckResult> ConnectAsync(string host, int port, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(host, port, timeout.Token);
            return HealthCheckResult.Healthy("The mail server is reachable.");
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("The mail server cannot be reached.");
        }
    }
}
