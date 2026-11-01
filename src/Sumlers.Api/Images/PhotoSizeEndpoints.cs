using System.Text.RegularExpressions;
using Sumlers.Api.Admin;
using Sumlers.Api.Data;
using Sumlers.Api.Site;

namespace Sumlers.Api.Images;

/// <summary>
/// Serves smaller copies of uploaded photos (/uploads/{name}-w960.webp), so a phone downloads a phone-sized photo.
/// Each copy is made the first time a page asks for it and saved next to the photo; after that the file is served as is.
/// </summary>
public static partial class PhotoSizeEndpoints
{
    /// <summary>
    /// The widths the pages ask for (web/src/app/shared/srcset.ts has the same list). Any other width is a 404, so
    /// requests cannot fill the disk. 680 and 760 match a typical phone, where Lighthouse also tests.
    /// </summary>
    public static readonly int[] Widths = [400, 680, 760, 960, 1280, 1920];

    // Lower than the uploaded photo's 80: the copies are what visitors download, and 70 looks the same on a screen.
    private const int Quality = 70;

    // Decoding a phone photo takes about 50 MB; one at a time keeps the hosting plan's memory safe.
    private static readonly SemaphoreSlim _OneAtATime = new(1, 1);

    public static void MapPhotoSizeEndpoints(this IEndpointRouteBuilder app) =>
        app.MapGet("/uploads/{name:regex(^[0-9a-f]{{32}}$)}-w{width:int}.webp", ServeAsync);

    /// <summary>The srcset for an uploaded photo, or null for any other address.</summary>
    public static string? Srcset(string? url)
    {
        var upload = UploadUrl().Match(url ?? "");
        return upload.Success ? string.Join(", ", Widths.Select(w => $"/uploads/{upload.Groups[1].Value}-w{w}.webp {w}w")) : null;
    }

    private static async Task<IResult> ServeAsync(string name, int width, AppDataFolder dataFolder, HttpContext context, CancellationToken ct)
    {
        if (!Widths.Contains(width)) return Results.NotFound();

        var directory = AdminEndpoints.UploadsDirectory(dataFolder);
        var path = Path.Combine(directory, $"{name}-w{width}.webp");
        if (!File.Exists(path) && !await CreateAsync(directory, name, width, path, ct)) return Results.NotFound();

        context.Response.Headers.CacheControl = StaticFileCaching.OneYear;
        return Results.File(path, "image/webp");
    }

    private static async Task<bool> CreateAsync(string directory, string name, int width, string path, CancellationToken ct)
    {
        var original = Directory.Exists(directory) ? Directory.EnumerateFiles(directory, name + ".*").FirstOrDefault() : null;
        if (original is null) return false;

        await _OneAtATime.WaitAsync(ct);
        try
        {
            if (File.Exists(path)) return true;
            await using var source = File.OpenRead(original);
            if (PhotoResizer.ToWebP(source, Quality, width) is not { } webp) return false;

            // Written under a temporary name, so a half-written copy is never served.
            var temporary = path + ".tmp";
            await File.WriteAllBytesAsync(temporary, webp, ct);
            File.Move(temporary, path, overwrite: true);
            return true;
        }
        finally
        {
            _OneAtATime.Release();
        }
    }

    [GeneratedRegex(@"^/uploads/([0-9a-f]{32})\.(jpe?g|png|webp)$", RegexOptions.IgnoreCase)]
    private static partial Regex UploadUrl();
}
