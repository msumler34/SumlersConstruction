using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Sumlers.Api.Options;

namespace Sumlers.Api.Tests;

public sealed class RedirectTests(SumlersApiFactory factory) : IClassFixture<SumlersApiFactory>
{
    private const string MainAddress = "https://sumlerhomes.com";

    [Theory]
    [InlineData("http://sumlerhomes.com/services?x=1")]
    [InlineData("https://www.sumlerhomes.com/services?x=1")]
    [InlineData("https://mattsumler-001-site1.ltempurl.com/services?x=1")]
    public async Task OtherAddresses_GoToTheMainAddress(string url)
    {
        await SetWebsiteAddressAsync(MainAddress);

        var response = await Client().GetAsync(url);

        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("https://sumlerhomes.com/services?x=1", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task MainAddress_IsServed()
    {
        await SetWebsiteAddressAsync(MainAddress);

        var response = await Client().GetAsync("https://sumlerhomes.com/services");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/admin")]
    [InlineData("/api/site")]
    [InlineData("/health")]
    [InlineData("/.well-known/acme-challenge/token")]
    [InlineData("/sitemap.xml")]
    public async Task AdminApiHealthCertificatesAndFiles_StayOnEveryAddress(string path)
    {
        await SetWebsiteAddressAsync(MainAddress);

        var response = await Client().GetAsync("http://mattsumler-001-site1.ltempurl.com" + path);

        Assert.NotEqual(HttpStatusCode.MovedPermanently, response.StatusCode);
    }

    [Fact]
    public async Task NoWebsiteAddress_NoRedirect()
    {
        await SetWebsiteAddressAsync("");

        var response = await Client().GetAsync("http://anything.example/services");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private async Task SetWebsiteAddressAsync(string url)
    {
        var admin = factory.CreateAdminClient();
        var site = (await admin.GetFromJsonAsync<SiteOptions>("/api/site"))!;
        site.Seo!.SiteUrl = url;
        (await admin.PutAsJsonAsync("/api/admin/site", site)).EnsureSuccessStatusCode();
    }
}
