using Sumlers.Api.Options;

namespace Sumlers.Api.Site;

/// <summary>
/// Brings a site.json saved before service pages existed up to date: its services get the default page
/// content and addresses, their old addresses redirect, and the new default services are added. The owner's
/// own names, descriptions and photos stay. It changes nothing on disk until the admin page saves.
/// </summary>
public static class ServicePagesUpgrade
{
    // The addresses the first version of the site used, and the service page each one became.
    private static readonly Dictionary<string, string> _RenamedKeys = new()
    {
        ["decks"] = "deck-construction",
        ["exterior"] = "porches-exteriors",
        ["repairs"] = "home-repairs",
    };

    // The store hands every request the same cached site, so two first requests must not both upgrade it.
    private static readonly Lock _Lock = new();

    public static void Apply(SiteOptions site, SiteOptions defaults)
    {
        if (site.ServicesPage is not null || ReferenceEquals(site, defaults)) return;
        lock (_Lock)
        {
            if (site.ServicesPage is null) Upgrade(site, defaults);
        }
    }

    private static void Upgrade(SiteOptions site, SiteOptions defaults)
    {
        foreach (var service in site.Services.Where(s => s.Page is null))
        {
            var newKey = _RenamedKeys.GetValueOrDefault(service.Key, service.Key);
            if (defaults.Services.FirstOrDefault(d => d.Key == newKey) is not { } match) continue;
            if (newKey != service.Key) service.PreviousKeys.Add(service.Key);
            service.Key = newKey;
            service.Page = match.Page;
        }

        var keys = site.Services.Select(s => s.Key).ToHashSet();
        site.Services.AddRange(defaults.Services.Where(d => !keys.Contains(d.Key)));
        site.ServicesPage = defaults.ServicesPage;
    }
}
