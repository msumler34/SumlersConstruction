using Sumlers.Api.Options;

namespace Sumlers.Api.Site;

/// <summary>What a page tells browsers and search engines: its title, description, address and structured data.</summary>
public sealed record PageMeta(
    int StatusCode,
    string Title,
    string Description,
    string CanonicalUrl,
    string ImageUrl,
    bool NoIndex,
    string? RedirectTo,
    Dictionary<string, object>? StructuredData);

/// <summary>
/// Builds the <see cref="PageMeta"/> for each public address. The server puts it into the page's HTML,
/// and the Angular app asks for it when it moves between pages, so both always agree.
/// </summary>
public static class PageMetaBuilder
{
    public static PageMeta For(SiteOptions site, string path, string baseUrl)
    {
        var key = path.Trim('/').ToLowerInvariant();
        return key switch
        {
            "" => Home(site, baseUrl),
            "services" => ServicesOverview(site, baseUrl),
            "contact" => Contact(site, baseUrl),
            "estimate" => Hidden(site, baseUrl, "Free Estimate", StatusCodes.Status301MovedPermanently) with { RedirectTo = "/contact" },
            "admin" => Hidden(site, baseUrl, "Admin", StatusCodes.Status200OK),
            _ => ForService(site, key, baseUrl),
        };
    }

    /// <summary>The site's main address: the one set in the admin page, or else the address of this request.</summary>
    public static string BaseUrl(SiteOptions site, HttpRequest request)
    {
        var configured = site.Seo?.SiteUrl.Trim().TrimEnd('/');
        return string.IsNullOrEmpty(configured) ? $"{request.Scheme}://{request.Host}" : configured;
    }

    /// <summary>Every public page address, for the sitemap.</summary>
    public static IEnumerable<string> PublicPaths(SiteOptions site) =>
        new[] { "/", "/services" }.Concat(site.Services.Select(s => "/" + s.Key)).Append("/contact");

    private static PageMeta Home(SiteOptions site, string baseUrl)
    {
        var title = Pick(site.Seo?.Title, site.BusinessName);
        var description = Pick(site.Seo?.Description, site.Tagline);
        return Page(site, baseUrl, "/", title, description, site.HeroImageUrl, Graph(Business(site, baseUrl)));
    }

    private static PageMeta ServicesOverview(SiteOptions site, string baseUrl)
    {
        var page = site.ServicesPage;
        var title = Pick(page?.SeoTitle, $"Services | {site.BusinessName}");
        var description = Pick(page?.SeoDescription, page?.Text, site.Seo?.Description);
        var crumbs = Breadcrumbs(baseUrl, ("Home", "/"), ("Services", "/services"));
        return Page(site, baseUrl, "/services", title, description, page?.ImageUrl, Graph(Business(site, baseUrl), crumbs));
    }

    private static PageMeta Contact(SiteOptions site, string baseUrl)
    {
        var description = $"Request a free, written estimate from {site.BusinessName}. Tell us about your project and we usually answer within a day.";
        return Page(site, baseUrl, "/contact", $"Free Estimate | {site.BusinessName}", description, null, Graph(Business(site, baseUrl)));
    }

    private static PageMeta ForService(SiteOptions site, string key, string baseUrl)
    {
        if (site.Services.FirstOrDefault(s => s.Key == key) is { } service) return Service(site, service, baseUrl);
        if (site.Services.FirstOrDefault(s => s.PreviousKeys.Contains(key)) is { } moved)
        {
            return Hidden(site, baseUrl, moved.Title, StatusCodes.Status301MovedPermanently) with { RedirectTo = "/" + moved.Key };
        }
        return Hidden(site, baseUrl, "Page not found", StatusCodes.Status404NotFound);
    }

    private static PageMeta Service(SiteOptions site, ServiceOffering service, string baseUrl)
    {
        var page = service.Page ?? new ServicePage();
        var path = "/" + service.Key;
        var title = Pick(page.SeoTitle, $"{service.Title} | {site.BusinessName}");
        var description = Pick(page.SeoDescription, service.Summary, page.Intro);
        var offer = Compact(new()
        {
            ["@type"] = "Service",
            ["name"] = service.Title,
            ["serviceType"] = service.Title,
            ["description"] = description,
            ["url"] = baseUrl + path,
            ["image"] = Absolute(service.ImageUrl, baseUrl),
            ["areaServed"] = site.ServiceArea,
            ["provider"] = new Dictionary<string, object> { ["@id"] = BusinessId(baseUrl) },
        });
        var crumbs = Breadcrumbs(baseUrl, ("Home", "/"), ("Services", "/services"), (service.Title, path));
        var graph = Graph(Business(site, baseUrl), offer, crumbs);
        if (page.Faqs.Count > 0) ((List<object>)graph["@graph"]).Add(FaqPage(page.Faqs));
        return Page(site, baseUrl, path, title, description, service.ImageUrl, graph);
    }

    private static PageMeta Page(SiteOptions site, string baseUrl, string path, string title, string description, string? image, Dictionary<string, object> data) =>
        new(StatusCodes.Status200OK, title, description, baseUrl + path, Absolute(Pick(image, site.HeroImageUrl), baseUrl), false, null, data);

    private static PageMeta Hidden(SiteOptions site, string baseUrl, string name, int status) =>
        new(status, $"{name} | {site.BusinessName}", site.Tagline, "", Absolute(site.HeroImageUrl, baseUrl), true, null, null);

    private static string BusinessId(string baseUrl) => baseUrl + "/#business";

    private static Dictionary<string, object> Business(SiteOptions site, string baseUrl) => Compact(new()
    {
        ["@type"] = "GeneralContractor",
        ["@id"] = BusinessId(baseUrl),
        ["name"] = site.BusinessName,
        ["url"] = baseUrl + "/",
        ["description"] = Pick(site.Seo?.Description, site.Tagline),
        ["telephone"] = site.Phone,
        ["email"] = site.Email,
        ["image"] = Absolute(site.HeroImageUrl, baseUrl),
        ["address"] = string.IsNullOrWhiteSpace(site.Address) ? null : new Dictionary<string, object> { ["@type"] = "PostalAddress", ["streetAddress"] = site.Address },
        ["areaServed"] = site.ServiceArea,
        ["sameAs"] = string.IsNullOrWhiteSpace(site.FacebookUrl) ? null : new[] { site.FacebookUrl },
    });

    private static Dictionary<string, object> Breadcrumbs(string baseUrl, params (string Name, string Path)[] crumbs) => new()
    {
        ["@type"] = "BreadcrumbList",
        ["itemListElement"] = crumbs.Select((c, i) => new Dictionary<string, object>
        {
            ["@type"] = "ListItem",
            ["position"] = i + 1,
            ["name"] = c.Name,
            ["item"] = baseUrl + c.Path,
        }).ToList(),
    };

    private static Dictionary<string, object> FaqPage(IEnumerable<Faq> faqs) => new()
    {
        ["@type"] = "FAQPage",
        ["mainEntity"] = faqs.Select(f => new Dictionary<string, object>
        {
            ["@type"] = "Question",
            ["name"] = f.Question,
            ["acceptedAnswer"] = new Dictionary<string, object> { ["@type"] = "Answer", ["text"] = f.Answer },
        }).ToList(),
    };

    private static Dictionary<string, object> Graph(params Dictionary<string, object>[] items) => new()
    {
        ["@context"] = "https://schema.org",
        ["@graph"] = items.Cast<object>().ToList(),
    };

    /// <summary>Leaves out blank values, so Google never sees an empty phone number or address.</summary>
    private static Dictionary<string, object> Compact(Dictionary<string, object?> values) =>
        values.Where(v => v.Value is not null && v.Value is not "").ToDictionary(v => v.Key, v => v.Value!);

    private static string Absolute(string? url, string baseUrl) =>
        string.IsNullOrWhiteSpace(url) ? "" : url.StartsWith('/') ? baseUrl + url : url;

    private static string Pick(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim() ?? "";
}
