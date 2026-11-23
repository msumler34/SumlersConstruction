using System.Text.RegularExpressions;
using Sumlers.Api.Options;

namespace Sumlers.Api.Site;

/// <summary>
/// The Google tag (gtag.js) for every page, plus a click-to-call hook that reports each tap on a tel: link.
/// Writes nothing until <see cref="AnalyticsOptions.GoogleTagId"/> is set.
/// </summary>
public static partial class GoogleTag
{
    public static string Tags(AnalyticsOptions? options)
    {
        var id = options?.GoogleTagId?.Trim() ?? "";
        if (!TagId().IsMatch(id)) return "";

        var label = options?.PhoneCallConversionLabel?.Trim() ?? "";
        var callEvent = Label().IsMatch(label) && id.StartsWith("AW-", StringComparison.Ordinal)
            ? $"gtag('event','conversion',{{send_to:'{id}/{label}'}});"
            : "gtag('event','click_to_call',{link_url:a.href});";

        return $"""<script async src="https://www.googletagmanager.com/gtag/js?id={id}"></script>"""
            + "<script>window.dataLayer=window.dataLayer||[];function gtag(){dataLayer.push(arguments);}"
            + $"gtag('js',new Date());gtag('config','{id}');"
            + "document.addEventListener('click',function(e){var a=e.target&&e.target.closest&&e.target.closest('a[href^=\"tel:\"]');"
            + $"if(a){{{callEvent}}}}},true);</script>";
    }

    // Only plain IDs reach the page, so a mistyped setting can never inject script.
    [GeneratedRegex(@"^(AW|G|GT)-[A-Z0-9]{4,20}$")]
    private static partial Regex TagId();

    [GeneratedRegex(@"^[A-Za-z0-9_-]{4,40}$")]
    private static partial Regex Label();
}
