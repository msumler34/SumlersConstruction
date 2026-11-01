using System.Text.Json;
using Sumlers.Api.Images;
using Sumlers.Api.Options;
using Sumlers.Api.Promotions;
using static System.Net.WebUtility;

namespace Sumlers.Api.Site;

/// <summary>
/// The site details and current specials, written into the page so the app can show them without first asking the
/// server, plus a preload of the home page photo so the browser fetches it before the app has started.
/// </summary>
public static class PageData
{
    public static string Tags(SiteOptions site, IEnumerable<Promotion> promotions, bool isHome)
    {
        // The same JSON as /api/site and /api/promotions. The default encoder escapes <, > and &, so page text can
        // never close the script tag.
        var json = JsonSerializer.Serialize(new { site, promotions }, JsonSerializerOptions.Web);
        var tags = $"""<script id="page-data" type="application/json">{json}</script>""";

        if (isHome && PhotoSizeEndpoints.Srcset(site.HeroImageUrl) is { } srcset)
        {
            tags += $"""<link rel="preload" as="image" imagesrcset="{HtmlEncode(srcset)}" imagesizes="100vw" fetchpriority="high">""";
        }
        return tags;
    }
}
