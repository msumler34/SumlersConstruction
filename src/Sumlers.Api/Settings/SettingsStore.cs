using Microsoft.Extensions.Options;
using Sumlers.Api.Data;
using Sumlers.Api.Options;

namespace Sumlers.Api.Settings;

/// <summary>Admin-only settings. Never served by the public API, because it holds the SMTP password.</summary>
public sealed class AdminSettings
{
    public EmailOptions Email { get; set; } = new();
    public AlertOptions Alerts { get; set; } = new();
}

public interface ISettingsStore
{
    Task<AdminSettings> GetAsync(CancellationToken cancellationToken);
    Task SaveAsync(AdminSettings settings, CancellationToken cancellationToken);
}

/// <summary>Serves App_Data/settings.json once the admin has saved it, and the appsettings.json values until then.</summary>
public sealed class JsonSettingsStore : ISettingsStore, IReloadable
{
    private readonly JsonDocument<AdminSettings> _Document;

    public JsonSettingsStore(AppDataFolder dataFolder, IOptions<EmailOptions> email, IOptions<AlertOptions> alerts)
    {
        _Document = new JsonDocument<AdminSettings>(
            dataFolder.PathFor("settings.json"),
            _ => Task.FromResult(new AdminSettings { Email = email.Value, Alerts = alerts.Value }));
    }

    public Task<AdminSettings> GetAsync(CancellationToken cancellationToken) => _Document.GetAsync(cancellationToken);

    public Task SaveAsync(AdminSettings settings, CancellationToken cancellationToken) => _Document.SetAsync(settings, cancellationToken);

    public void Reload() => _Document.Reload();
}
