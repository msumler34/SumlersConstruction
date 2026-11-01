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
                _Html = WithoutHeadTags(reader.ReadToEnd());
                _LoadedFrom = file.LastModified;
            }
            return _Html;
        }
    }

    /// <summary>Removes the tags that each page writes for itself, so they never appear twice.</summary>
    public static string WithoutHeadTags(string html) => PageTags().Replace(html, "");

    [GeneratedRegex("""\s*(<title>.*?</title>|<meta\s+(name="description"|property="og:[^"]*"|name="twitter:[^"]*")[^>]*>)""", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex PageTags();
}
