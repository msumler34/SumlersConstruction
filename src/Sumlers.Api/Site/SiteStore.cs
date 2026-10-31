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
public sealed class JsonSiteStore(IOptions<AdminOptions> adminOptions, IOptionsMonitor<SiteOptions> defaults, IWebHostEnvironment environment)
    : ISiteStore
{
    private readonly SemaphoreSlim _Lock = new(1, 1);
    private readonly string _FilePath = Path.Combine(environment.ContentRootPath, adminOptions.Value.DataDirectory, "site.json");
    private SiteOptions? _Saved;
    private bool _Loaded;

    public async Task<SiteOptions> GetAsync(CancellationToken cancellationToken)
    {
        await _Lock.WaitAsync(cancellationToken);
        try
        {
            if (!_Loaded)
            {
                _Saved = await JsonFile.ReadAsync<SiteOptions>(_FilePath, cancellationToken);
                _Loaded = true;
            }
            return _Saved ?? defaults.CurrentValue;
        }
        finally
        {
            _Lock.Release();
        }
    }

    public async Task SaveAsync(SiteOptions site, CancellationToken cancellationToken)
    {
        await _Lock.WaitAsync(cancellationToken);
        try
        {
            await JsonFile.WriteAsync(_FilePath, site, cancellationToken);
            _Saved = site;
            _Loaded = true;
        }
        finally
        {
            _Lock.Release();
        }
    }
}
