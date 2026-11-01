namespace Sumlers.Api.Site;

/// <summary>
/// Once the admin page sets the website address, sends every other address (http://, www. and the hosting
/// company's temporary address) to it with a permanent redirect, so visitors always get https and Google
/// sees one copy of the site.
/// </summary>
public sealed class MainAddressRedirect(RequestDelegate next, ISiteStore store)
{
    // Always reachable on any address: the admin page (to fix a wrong website address), the API, the health
    // check, and the path the free SSL certificate is validated and renewed through.
    private static readonly PathString[] _AlwaysServed = ["/admin", "/api", "/health", "/.well-known"];

    public async Task InvokeAsync(HttpContext context)
    {
        if (await RedirectTargetAsync(context) is { } target)
        {
            context.Response.Redirect(target, permanent: true);
            return;
        }
        await next(context);
    }

    private async Task<string?> RedirectTargetAsync(HttpContext context)
    {
        var request = context.Request;
        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method)) return null;
        if (_AlwaysServed.Any(path => request.Path.StartsWithSegments(path))) return null;

        // Only pages move. Files (scripts, photos, the sitemap) load on any address, so the admin page still
        // works on the temporary address if the main one ever breaks.
        if (Path.HasExtension(request.Path.Value)) return null;

        var site = await store.GetAsync(context.RequestAborted);
        if (!Uri.TryCreate(site.Seo?.SiteUrl, UriKind.Absolute, out var main)) return null;

        var sameAddress = request.Scheme == main.Scheme && string.Equals(request.Host.Value, main.Authority, StringComparison.OrdinalIgnoreCase);
        return sameAddress ? null : $"{main.Scheme}://{main.Authority}{request.PathBase}{request.Path}{request.QueryString}";
    }
}
