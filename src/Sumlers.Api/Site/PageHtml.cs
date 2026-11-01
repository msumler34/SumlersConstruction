using System.Text;
using System.Text.Json;
using static System.Net.WebUtility;

namespace Sumlers.Api.Site;

/// <summary>Writes a page's <see cref="PageMeta"/> into the head of the Angular index.html.</summary>
public static class PageHtml
{
    public static string Render(string template, PageMeta meta, string businessName)
    {
        var tags = new StringBuilder();
        tags.Append($"<title>{HtmlEncode(meta.Title)}</title>");
        Meta(tags, "name", "description", meta.Description);
        if (meta.NoIndex) Meta(tags, "name", "robots", "noindex");
        if (meta.CanonicalUrl.Length > 0) tags.Append($"""<link rel="canonical" href="{HtmlEncode(meta.CanonicalUrl)}">""");
        Meta(tags, "property", "og:type", "website");
        Meta(tags, "property", "og:site_name", businessName);
        Meta(tags, "property", "og:title", meta.Title);
        Meta(tags, "property", "og:description", meta.Description);
        Meta(tags, "property", "og:url", meta.CanonicalUrl);
        Meta(tags, "property", "og:image", meta.ImageUrl);
        Meta(tags, "name", "twitter:card", "summary_large_image");

        // The default encoder escapes <, > and &, so page text can never close the script tag.
        if (meta.StructuredData is { } data) tags.Append($"""<script type="application/ld+json">{JsonSerializer.Serialize(data)}</script>""");

        var headEnd = template.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
        return headEnd < 0 ? tags + template : template.Insert(headEnd, tags.ToString());
    }

    private static void Meta(StringBuilder tags, string attribute, string name, string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return;
        tags.Append($"""<meta {attribute}="{name}" content="{HtmlEncode(content)}">""");
    }
}
