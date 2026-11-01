using Microsoft.Extensions.Options;
using Sumlers.Api.Data;
using Sumlers.Api.Options;

namespace Sumlers.Api.Site;

public interface ISiteStore
{
    Task<SiteOptions> GetAsync(CancellationToken cancellationToken);
    Task SaveAsync(SiteOptions site, CancellationToken cancellationToken);
}

/// <summary>Serves App_Data/site.json when the admin has saved one, and the appsettings.json defaults until then.</summary>
public sealed class JsonSiteStore : ISiteStore, IReloadable
{
    private readonly JsonDocument<SiteOptions> _Document;
    private readonly IOptionsMonitor<SiteOptions> _Defaults;

    public JsonSiteStore(AppDataFolder dataFolder, IOptionsMonitor<SiteOptions> defaults)
    {
        _Defaults = defaults;
        _Document = new JsonDocument<SiteOptions>(dataFolder.PathFor("site.json"), _ => Task.FromResult(defaults.CurrentValue));
    }

    public async Task<SiteOptions> GetAsync(CancellationToken cancellationToken)
    {
        var site = await _Document.GetAsync(cancellationToken);
        ServicePagesUpgrade.Apply(site, _Defaults.CurrentValue);
        return site.WithMissingSectionsFrom(_Defaults.CurrentValue);
    }

    public Task SaveAsync(SiteOptions site, CancellationToken cancellationToken) => _Document.SetAsync(site, cancellationToken);

    public void Reload() => _Document.Reload();
}
