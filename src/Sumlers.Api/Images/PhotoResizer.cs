using SkiaSharp;

namespace Sumlers.Api.Images;

/// <summary>Turns a phone photo into a WebP small enough for the web, the right way up and without its camera data.</summary>
public static class PhotoResizer
{
    private static readonly SKSamplingOptions _Sampling = new(SKCubicResampler.Mitchell);

    /// <summary>The photo as WebP, scaled down to fit the limits, or null when the data is not an image.</summary>
    public static byte[]? ToWebP(Stream source, int quality, int maxWidth, int maxHeight = int.MaxValue)
    {
        using var codec = SKCodec.Create(source);
        if (codec is null) return null;
        using var decoded = SKBitmap.Decode(codec);
        if (decoded is null) return null;

        using var turned = Turned(decoded, codec.EncodedOrigin);
        var upright = turned ?? decoded;
        var scale = Math.Min(1.0, Math.Min((double)maxWidth / upright.Width, (double)maxHeight / upright.Height));
        using var resized = scale < 1 ? upright.Resize(Scaled(upright, scale), _Sampling) : null;

        using var image = SKImage.FromBitmap(resized ?? upright);
        using var data = image.Encode(SKEncodedImageFormat.Webp, quality);
        return data.ToArray();
    }

    private static SKImageInfo Scaled(SKBitmap bitmap, double scale) =>
        new(Math.Max(1, (int)Math.Round(bitmap.Width * scale)), Math.Max(1, (int)Math.Round(bitmap.Height * scale)));

    // Phones save many photos sideways and record which way is up. The WebP has no such note, so turn the pixels.
    private static SKBitmap? Turned(SKBitmap bitmap, SKEncodedOrigin origin)
    {
        if (origin == SKEncodedOrigin.TopLeft) return null;

        var sideways = origin >= SKEncodedOrigin.LeftTop;
        var turned = new SKBitmap(sideways ? bitmap.Height : bitmap.Width, sideways ? bitmap.Width : bitmap.Height);
        using var canvas = new SKCanvas(turned);
        canvas.SetMatrix(Upright(origin, bitmap.Width, bitmap.Height));
        canvas.DrawBitmap(bitmap, 0, 0);
        return turned;
    }

    private static SKMatrix Upright(SKEncodedOrigin origin, float width, float height) => origin switch
    {
        SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, width, 0, 1, 0, 0, 0, 1),
        SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, width, 0, -1, height, 0, 0, 1),
        SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, height, 0, 0, 1),
        SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightTop => new SKMatrix(0, -1, height, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, height, -1, 0, width, 0, 0, 1),
        SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, width, 0, 0, 1),
        _ => SKMatrix.Identity,
    };
}
