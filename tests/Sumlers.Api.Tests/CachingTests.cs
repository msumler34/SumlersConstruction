using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Sumlers.Api.Site;

namespace Sumlers.Api.Tests;

public sealed class CachingTests(SumlersApiFactory factory) : IClassFixture<SumlersApiFactory>
{
    [Theory]
    [InlineData("main-3OHA54OT.js", "public, max-age=31536000, immutable")]
    [InlineData("styles-QQG32EKV.css", "public, max-age=31536000, immutable")]
    [InlineData("inter-latin-wght-normal-NRMW37G5.woff2", "public, max-age=31536000, immutable")]
    [InlineData("favicon.svg", "public, max-age=86400")]
    [InlineData("google5a456730e7374967.html", "public, max-age=86400")]
    [InlineData("index.html", "no-cache")]
    public void WebRootFiles_AreKeptAsLongAsTheirNameIsSafe(string name, string expected)
    {
        var context = new DefaultHttpContext();

        StaticFileCaching.ForWebRoot(new StaticFileResponseContext(context, new NotFoundFileInfo(name)));

        Assert.Equal(expected, context.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task UploadedPhotos_AreKeptForAYear()
    {
        var uploads = Path.Combine(factory.DataDirectory, "uploads");
        Directory.CreateDirectory(uploads);
        await File.WriteAllBytesAsync(Path.Combine(uploads, "photo.jpg"), [0xFF, 0xD8, 0xFF]);

        var response = await factory.CreateClient().GetAsync("/uploads/photo.jpg");

        response.EnsureSuccessStatusCode();
        Assert.Equal("public, max-age=31536000, immutable", response.Headers.CacheControl?.ToString());
    }
}
