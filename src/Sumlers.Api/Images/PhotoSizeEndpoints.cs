using Sumlers.Api.Admin;
using Sumlers.Api.Data;
using Sumlers.Api.Site;

namespace Sumlers.Api.Images;

/// <summary>
/// Serves smaller copies of uploaded photos (/uploads/{name}-960.webp), so a phone downloads a phone-sized photo.
/// Each copy is made the first time a page asks for it and saved next to the photo; after that the file is served as is.
/// </summary>
public static class PhotoSizeEndpoints
{
    /// <summary>The widths the pages ask for. Any other width is a 404, so requests cannot fill the disk.</summary>
    public static readonly int[] Widths = [480, 960, 1440, 1920];

    // Decoding a phone photo takes about 50 MB; one at a time keeps the hosting plan's memory safe.
    private static readonly SemaphoreSlim _OneAtATime = new(1, 1);

    public static void MapPhotoSizeEndpoints(this IEndpointRouteBuilder app) =>
        app.MapGet("/uploads/{name:regex(^[0-9a-f]{{32}}$)}-{width:int}.webp", ServeAsync);

    private static async Task<IResult> ServeAsync(string name, int width, AppDataFolder dataFolder, HttpContext context, CancellationToken ct)
    {
        if (!Widths.Contains(width)) return Results.NotFound();

        var directory = AdminEndpoints.UploadsDirectory(dataFolder);
        var path = Path.Combine(directory, $"{name}-{width}.webp");
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
            if (PhotoResizer.ToWebP(source, width) is not { } webp) return false;

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
}
