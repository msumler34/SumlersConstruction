using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Sumlers.Api.Options;
using Sumlers.Api.Site;

namespace Sumlers.Api.Tests;

public sealed class PageTests(SumlersApiFactory factory) : IClassFixture<SumlersApiFactory>
{
    [Fact]
    public async Task ServicePage_HasItsOwnTitleCanonicalAndStructuredData()
    {
        var response = await Client().GetAsync("/deck-construction");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<title>Custom Deck Builders | Sumlers Construction</title>", html);
        Assert.Contains("""<link rel="canonical" href="http://localhost/deck-construction">""", html);
        Assert.Contains("\"GeneralContractor\"", html);
        Assert.Contains("\"FAQPage\"", html);
        Assert.Single(html.Split("<title>").Skip(1));
        Assert.DoesNotContain("Old description", html);
        Assert.Equal("no-cache", response.Headers.CacheControl?.ToString());
    }

    [Theory]
    [InlineData("/", "Sumlers Construction | Custom Decks & Home Remodeling")]
    [InlineData("/services", "Decks, Remodeling & Home Repair Services | Sumlers Construction")]
    [InlineData("/contact", "Free Estimate | Sumlers Construction")]
    public async Task MainPages_HaveTheirTitles(string path, string title)
    {
        var html = await Client().GetStringAsync(path);

        Assert.Contains($"<title>{WebUtility.HtmlEncode(title)}</title>", html);
    }

    [Fact]
    public async Task UnknownPage_Returns404ThatSearchEnginesSkip()
    {
        var response = await Client().GetAsync("/no-such-page");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("""<meta name="robots" content="noindex">""", html);
        Assert.DoesNotContain("rel=\"canonical\"", html);
    }

    [Fact]
    public async Task MissingFile_IsAPlain404()
    {
        var response = await Client().GetAsync("/chunk-missing.js");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("<app-root>", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task RenamedService_OldAddressRedirectsToTheNewOne()
    {
        var site = await SiteAsync();
        var repairs = site.Services.Single(s => s.Key == "home-repairs");
        repairs.Key = "repairs";
        repairs.PreviousKeys.Add("home-repairs");
        await SaveAsync(site);

        var response = await Client().GetAsync("/home-repairs?ref=old");

        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("/repairs?ref=old", response.Headers.Location?.ToString());
        Assert.Equal(HttpStatusCode.OK, (await Client().GetAsync("/repairs")).StatusCode);
    }

    [Fact]
    public async Task SiteSave_KeepsServicesOffAddressesTheSiteUses()
    {
        var site = await SiteAsync();
        site.Services.Add(new ServiceOffering { Title = "Contact", Key = "contact", PreviousKeys = ["kitchen-remodeling"] });

        var saved = await SaveAsync(site);

        var added = saved.Services.Last();
        Assert.Equal("contact-2", added.Key);
        Assert.Empty(added.PreviousKeys);
    }

    [Fact]
    public async Task ServiceText_CannotBreakOutOfTheStructuredData()
    {
        var site = await SiteAsync();
        site.Services.Add(new ServiceOffering { Title = "Evil</script><script>alert(1)</script>", Key = "evil" });
        await SaveAsync(site);

        var html = await Client().GetStringAsync("/evil");

        Assert.DoesNotContain("<script>alert(1)", html);
        Assert.Contains("Evil&lt;/script&gt;", html);
    }

    [Theory]
    [InlineData("http://sumlerhomes.com")]
    [InlineData("https://sumlerhomes.com/home")]
    [InlineData("sumlerhomes.com")]
    public async Task SiteSave_RejectsWebsiteAddressesThatAreNotJustHttpsAndAName(string url)
    {
        var site = await SiteAsync();
        site.Seo!.SiteUrl = url;

        var response = await factory.CreateAdminClient().PutAsJsonAsync("/api/admin/site", site);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Sitemap_ListsEveryPublicPage()
    {
        var xml = await Client().GetStringAsync("/sitemap.xml");

        Assert.Contains("<loc>http://localhost/</loc>", xml);
        Assert.Contains("<loc>http://localhost/services</loc>", xml);
        Assert.Contains("<loc>http://localhost/kitchen-remodeling</loc>", xml);
        Assert.Contains("<loc>http://localhost/contact</loc>", xml);
        Assert.DoesNotContain("/admin", xml);
    }

    [Fact]
    public async Task Robots_HidesAdminAndPointsToTheSitemap()
    {
        var robots = await Client().GetStringAsync("/robots.txt");

        Assert.Contains("Disallow: /admin", robots);
        Assert.Contains("Sitemap: http://localhost/sitemap.xml", robots);
    }

    [Fact]
    public async Task PageMeta_MatchesWhatTheServerWritesIntoThePage()
    {
        var meta = await Client().GetFromJsonAsync<PageMeta>("/api/page-meta?path=/bathroom-remodeling");

        Assert.Equal("Bathroom Remodeling Contractor | Sumlers Construction", meta!.Title);
        Assert.Equal("http://localhost/bathroom-remodeling", meta.CanonicalUrl);
        Assert.Null(meta.StructuredData);
    }

    [Fact]
    public async Task SiteSavedBeforeServicePages_GetsThePagesAndKeepsTheOwnersText()
    {
        using var legacy = new SumlersApiFactory();
        Directory.CreateDirectory(legacy.DataDirectory);
        await File.WriteAllTextAsync(Path.Combine(legacy.DataDirectory, "site.json"), """
            {
              "BusinessName": "Sumlers Construction",
              "Services": [
                { "Key": "decks", "Title": "Decks", "Summary": "The owner's own deck text." },
                { "Key": "patios", "Title": "Patios", "Summary": "A service the owner added." }
              ]
            }
            """);
        var client = legacy.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var site = (await client.GetFromJsonAsync<SiteOptions>("/api/site"))!;
        var decks = site.Services[0];
        var redirect = await client.GetAsync("/decks");

        Assert.Equal("deck-construction", decks.Key);
        Assert.Equal("The owner's own deck text.", decks.Summary);
        Assert.Equal(["decks"], decks.PreviousKeys);
        Assert.NotEmpty(decks.Page!.Faqs);
        Assert.Equal("patios", site.Services[1].Key);
        Assert.Contains(site.Services, s => s.Key == "kitchen-remodeling");
        Assert.Single(site.Services, s => s.Key == "deck-construction");
        Assert.NotNull(site.ServicesPage);
        Assert.Equal("/deck-construction", redirect.Headers.Location?.ToString());
    }

    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private async Task<SiteOptions> SiteAsync() => (await Client().GetFromJsonAsync<SiteOptions>("/api/site"))!;

    private async Task<SiteOptions> SaveAsync(SiteOptions site)
    {
        var response = await factory.CreateAdminClient().PutAsJsonAsync("/api/admin/site", site);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SiteOptions>())!;
    }
}
