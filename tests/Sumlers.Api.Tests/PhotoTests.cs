using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SkiaSharp;
using Sumlers.Api.Images;

namespace Sumlers.Api.Tests;

public sealed class PhotoTests(SumlersApiFactory factory) : IClassFixture<SumlersApiFactory>
{
    // 40x20 JPEG, left half red and right half blue, saved with EXIF orientation 6 (turn 90° clockwise to view).
    private const string SidewaysJpeg = "/9j/4AAQSkZJRgABAQAAAQABAAD/4QAiRXhpZgAATU0AKgAAAAgAAQESAAMAAAABAAYAAAAAAAD/2wBDAAIBAQEBAQIBAQECAgICAgQDAgICAgUEBAMEBgUGBgYFBgYGBwkIBgcJBwYGCAsICQoKCgoKBggLDAsKDAkKCgr/2wBDAQICAgICAgUDAwUKBwYHCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgr/wAARCAAUACgDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwD4vooor+Uz/fw8h/aq/wCYD/29f+0a8hr179qr/mA/9vX/ALRryGv9/voS/wDKMWQf9zX/AKm4k/xD+l9/ykTnf/ct/wComHCiiiv6qP5rPryiiiv+Vc/6UDyH9qr/AJgP/b1/7RryGiiv9/voS/8AKMWQf9zX/qbiT/EP6X3/ACkTnf8A3Lf+omHCiiiv6qP5rP/Z";

    [Fact]
    public async Task Upload_SavesASmallerWebP()
    {
        var response = await UploadAsync(Jpeg(4000, 3000), "image/jpeg");

        response.EnsureSuccessStatusCode();
        var url = (await response.Content.ReadFromJsonAsync<UploadResult>())!.Url;
        Assert.Matches("^/uploads/[0-9a-f]{32}\\.webp$", url);
        using var saved = SKBitmap.Decode(await factory.CreateClient().GetByteArrayAsync(url));
        Assert.Equal((2560, 1920), (saved.Width, saved.Height));
    }

    [Fact]
    public async Task Upload_RejectsAFileThatIsNotAnImage()
    {
        var response = await UploadAsync("not a photo"u8.ToArray(), "image/jpeg");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void SidewaysPhoto_IsTurnedTheRightWayUp()
    {
        using var source = new MemoryStream(Convert.FromBase64String(SidewaysJpeg));

        using var turned = SKBitmap.Decode(PhotoResizer.ToWebP(source, quality: 70, 1000)!);

        Assert.Equal((20, 40), (turned.Width, turned.Height));
        Assert.True(turned.GetPixel(10, 5).Red > 200, "The red half should be on top.");
        Assert.True(turned.GetPixel(10, 35).Blue > 200, "The blue half should be at the bottom.");
    }

    [Fact]
    public async Task SmallerCopy_IsMadeFromTheUploadAndCached()
    {
        var name = SaveUpload(Jpeg(2000, 1000), ".jpg");

        var response = await factory.CreateClient().GetAsync($"/uploads/{name}-w960.webp");

        response.EnsureSuccessStatusCode();
        Assert.Equal("image/webp", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("public, max-age=31536000, immutable", response.Headers.CacheControl?.ToString());
        using var copy = SKBitmap.Decode(await response.Content.ReadAsByteArrayAsync());
        Assert.Equal((960, 480), (copy.Width, copy.Height));
        Assert.True(File.Exists(Path.Combine(factory.DataDirectory, "uploads", $"{name}-w960.webp")));
    }

    [Theory]
    [InlineData("777")]
    [InlineData("960", "0123456789abcdef0123456789abcdef")]
    public async Task SmallerCopy_OnlyForKnownWidthsAndUploads(string width, string? name = null)
    {
        name ??= SaveUpload(Jpeg(100, 100), ".jpg");

        var response = await factory.CreateClient().GetAsync($"/uploads/{name}-w{width}.webp");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<HttpResponseMessage> UploadAsync(byte[] bytes, string contentType)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        using var form = new MultipartFormDataContent { { file, "file", "photo.jpg" } };
        return await factory.CreateAdminClient().PostAsync("/api/admin/uploads", form);
    }

    private string SaveUpload(byte[] bytes, string extension)
    {
        var name = Guid.NewGuid().ToString("N");
        var uploads = Path.Combine(factory.DataDirectory, "uploads");
        Directory.CreateDirectory(uploads);
        File.WriteAllBytes(Path.Combine(uploads, name + extension), bytes);
        return name;
    }

    private static byte[] Jpeg(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.SaddleBrown);
        using var image = SKImage.FromBitmap(bitmap);
        return image.Encode(SKEncodedImageFormat.Jpeg, 80).ToArray();
    }

    private sealed record UploadResult(string Url);
}
