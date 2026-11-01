using System.Text;
using System.Xml.Linq;
using Sumlers.Api.Promotions;

namespace Sumlers.Api.Site;

/// <summary>
/// Serves the page addresses (/, /services, /decks…) with each page's own title, description and
/// structured data already in the HTML, plus the sitemap and robots.txt that search engines read.
/// </summary>
public static class PageEndpoints
{
    public static void MapPageEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/page-meta", GetMetaAsync);
        app.MapGet("/sitemap.xml", GetSitemapAsync);
        app.MapGet("/robots.txt", GetRobotsAsync);

        // Only addresses without a file extension, so a missing .js or .png file stays a plain 404.
        app.MapFallback("{*path:nonfile}", ServePageAsync);
    }

    private static async Task<IResult> GetMetaAsync(string? path, HttpRequest request, ISiteStore store, CancellationToken ct)
    {
        var site = await store.GetAsync(ct);
        var meta = PageMetaBuilder.For(site, path ?? "/", PageMetaBuilder.BaseUrl(site, request));
        return Results.Ok(meta with { StructuredData = null });
    }

    private static async Task<IResult> ServePageAsync(
        HttpContext context, ISiteStore store, IPromotionStore promotionStore, TimeProvider clock, IPageTemplate template, CancellationToken ct)
    {
        var html = template.Get();
        if (html is null) return Results.NotFound();

        var site = await store.GetAsync(ct);
        var meta = PageMetaBuilder.For(site, context.Request.Path, PageMetaBuilder.BaseUrl(site, context.Request));
        if (meta.RedirectTo is { } target) return Results.Redirect(target + context.Request.QueryString, permanent: true);

        // Pages change whenever the owner saves, and a deploy changes the script names in the HTML.
        context.Response.Headers.CacheControl = "no-cache";
        var promotions = await SiteEndpoints.ShownPromotionsAsync(promotionStore, clock, ct);
        var data = PageData.Tags(site, promotions, isHome: context.Request.Path == "/");
        return Results.Content(PageHtml.Render(html, meta, site.BusinessName, data), "text/html; charset=utf-8", Encoding.UTF8, meta.StatusCode);
    }

    private static async Task<IResult> GetSitemapAsync(HttpRequest request, ISiteStore store, CancellationToken ct)
    {
        var site = await store.GetAsync(ct);
        var baseUrl = PageMetaBuilder.BaseUrl(site, request);
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var urls = PageMetaBuilder.PublicPaths(site).Select(path => new XElement(ns + "url", new XElement(ns + "loc", baseUrl + path)));
        var xml = new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement(ns + "urlset", urls));
        return Results.Content(xml.Declaration + "\n" + xml.Root, "application/xml", Encoding.UTF8);
    }

    private static async Task<IResult> GetRobotsAsync(HttpRequest request, ISiteStore store, CancellationToken ct)
    {
        var site = await store.GetAsync(ct);
        var robots = $"""
            User-agent: *
            Disallow: /admin
            Disallow: /api/

            Sitemap: {PageMetaBuilder.BaseUrl(site, request)}/sitemap.xml

            """;
        return Results.Text(robots, "text/plain", Encoding.UTF8);
    }
}
