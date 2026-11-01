using System.Text.RegularExpressions;
using Microsoft.AspNetCore.StaticFiles;

namespace Sumlers.Api.Site;

/// <summary>How long browsers keep each file, so a repeat visit loads the photos, scripts and fonts from its cache.</summary>
public static partial class StaticFileCaching
{
    public const string OneYear = "public, max-age=31536000, immutable";
    private const string OneDay = "public, max-age=86400";

    /// <summary>Each build gives a changed script, style or font a new name, so a named file never changes.</summary>
    public static void ForWebRoot(StaticFileResponseContext context)
    {
        var name = context.File.Name;
        var cacheControl = name == "index.html" ? "no-cache" : BuiltName().IsMatch(name) ? OneYear : OneDay;
        context.Context.Response.Headers.CacheControl = cacheControl;
    }

    /// <summary>Every upload gets a new random name, so an uploaded photo never changes either.</summary>
    public static void ForUploads(StaticFileResponseContext context) => context.Context.Response.Headers.CacheControl = OneYear;

    // Angular's build names: main-3OHA54OT.js, styles-QQG32EKV.css, media/inter-latin-wght-normal-NRMW37G5.woff2.
    [GeneratedRegex(@"-[A-Z0-9]{8}\.[a-z0-9]+$")]
    private static partial Regex BuiltName();
}
