using System.Text.RegularExpressions;

namespace Sumlers.Api.Site;

/// <summary>The Angular app's index.html, which every page address serves.</summary>
public interface IPageTemplate
{
    /// <summary>The HTML without its own title and description tags, or null before the Angular app is built.</summary>
    string? Get();
}

/// <summary>Reads wwwroot/index.html once, and again whenever a deploy replaces it.</summary>
public sealed partial class WebRootPageTemplate(IWebHostEnvironment environment) : IPageTemplate
{
    private readonly Lock _Lock = new();
    private DateTimeOffset _LoadedFrom;
    private string? _Html;

    public string? Get()
    {
        var file = environment.WebRootFileProvider.GetFileInfo("index.html");
        if (!file.Exists) return null;

        lock (_Lock)
        {
            if (_Html is null || file.LastModified != _LoadedFrom)
            {
                using var reader = new StreamReader(file.CreateReadStream());
                _Html = WithFontPreloads(WithoutHeadTags(reader.ReadToEnd()), FontFiles());
                _LoadedFrom = file.LastModified;
            }
            return _Html;
        }
    }

    // The two fonts the page always uses. The browser only finds them in the stylesheet, so without a preload it
    // waits for the stylesheet before it starts on them.
    private IEnumerable<string> FontFiles() =>
        environment.WebRootFileProvider.GetDirectoryContents("media").Select(file => file.Name).Where(name => PreloadedFont().IsMatch(name));

    /// <summary>Adds a preload for each font file, named as the Angular build names them in /media.</summary>
    public static string WithFontPreloads(string html, IEnumerable<string> fontFiles)
    {
        var links = string.Concat(fontFiles.Order().Select(name => $"""<link rel="preload" href="media/{name}" as="font" type="font/woff2" crossorigin>"""));
        var headEnd = html.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
        return headEnd < 0 || links.Length == 0 ? html : html.Insert(headEnd, links);
    }

    /// <summary>Removes the tags that each page writes for itself, so they never appear twice.</summary>
    public static string WithoutHeadTags(string html) => PageTags().Replace(html, "");

    [GeneratedRegex("""\s*(<title>.*?</title>|<meta\s+(name="description"|property="og:[^"]*"|name="twitter:[^"]*")[^>]*>)""", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex PageTags();

    [GeneratedRegex(@"^(fraunces-latin-opsz-normal|inter-latin-wght-normal)-[A-Z0-9]{8}\.woff2$")]
    private static partial Regex PreloadedFont();
}
