using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Sumlers.Api.Options;
using Sumlers.Api.Site;

namespace Sumlers.Api.Tests;

public sealed class PageDataTests
{
    [Fact]
    public async Task Page_CarriesTheSameSiteDetailsAsTheApi()
    {
        using var factory = new SumlersApiFactory();
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/services");
        var api = await client.GetStringAsync("/api/site");

        var data = JsonNode.Parse(PageDataJson(html))!;
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(api), data["site"]));
        Assert.IsType<JsonArray>(data["promotions"]);
    }

    [Fact]
    public async Task Page_TextCannotCloseTheDataScript()
    {
        using var factory = new SumlersApiFactory();
        var site = (await factory.CreateClient().GetFromJsonAsync<SiteOptions>("/api/site"))!;
        site.Tagline = "Decks </script><script>alert(1)</script>";
        (await factory.CreateAdminClient().PutAsJsonAsync("/api/admin/site", site)).EnsureSuccessStatusCode();

        var html = await factory.CreateClient().GetStringAsync("/");

        Assert.DoesNotContain("<script>alert(1)", html);
        Assert.Contains("alert(1)", PageDataJson(html));
    }

    [Fact]
    public async Task HomePage_PreloadsAnUploadedHeroPhoto()
    {
        using var factory = new SumlersApiFactory();
        var site = (await factory.CreateClient().GetFromJsonAsync<SiteOptions>("/api/site"))!;
        site.HeroImageUrl = "/uploads/0123456789abcdef0123456789abcdef.jpg";
        (await factory.CreateAdminClient().PutAsJsonAsync("/api/admin/site", site)).EnsureSuccessStatusCode();

        var home = await factory.CreateClient().GetStringAsync("/");
        var contact = await factory.CreateClient().GetStringAsync("/contact");

        Assert.Contains("""<link rel="preload" as="image" imagesrcset="/uploads/0123456789abcdef0123456789abcdef-w400.webp 400w""", home);
        Assert.DoesNotContain("""rel="preload" as="image""", contact);
    }

    [Fact]
    public void Template_PreloadsTheFonts()
    {
        var html = WebRootPageTemplate.WithFontPreloads("<head></head>", ["inter-latin-wght-normal-NRMW37G5.woff2"]);

        Assert.Equal("""<head><link rel="preload" href="media/inter-latin-wght-normal-NRMW37G5.woff2" as="font" type="font/woff2" crossorigin></head>""", html);
    }

    private static string PageDataJson(string html) =>
        Regex.Match(html, """<script id="page-data" type="application/json">(.*?)</script>""", RegexOptions.Singleline).Groups[1].Value;
}
