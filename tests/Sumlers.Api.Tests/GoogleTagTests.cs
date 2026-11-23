using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Sumlers.Api.Options;
using Sumlers.Api.Site;

namespace Sumlers.Api.Tests;

public sealed class GoogleTagTests(SumlersApiFactory factory) : IClassFixture<SumlersApiFactory>
{
    [Fact]
    public async Task NoTagId_NoGoogleScript()
    {
        var html = await factory.CreateClient().GetStringAsync("/");

        Assert.DoesNotContain("googletagmanager", html);
    }

    [Fact]
    public void TagId_AddsGtagAndCallConversion()
    {
        var tags = GoogleTag.Tags(new AnalyticsOptions { GoogleTagId = "AW-123456789", PhoneCallConversionLabel = "AbC-dEf_1" });

        Assert.Contains("https://www.googletagmanager.com/gtag/js?id=AW-123456789", tags);
        Assert.Contains("gtag('config','AW-123456789')", tags);
        Assert.Contains("send_to:'AW-123456789/AbC-dEf_1'", tags);
        Assert.Contains("a[href^=\"tel:\"]", tags);
    }

    [Theory]
    [InlineData("")]
    [InlineData("AW-1');alert(1);//")]
    [InlineData("not-a-tag")]
    public void BadOrEmptyTagId_WritesNothing(string id) =>
        Assert.Equal("", GoogleTag.Tags(new AnalyticsOptions { GoogleTagId = id }));

    [Theory]
    [InlineData("/index.html")]
    [InlineData("/INDEX.HTML")]
    public async Task IndexHtml_RedirectsToHomePage(string path)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.OriginalString);
    }
}

public sealed class FaviconTests(SumlersApiFactory factory) : IClassFixture<SumlersApiFactory>
{
    [Theory]
    [InlineData("/favicon.ico", "image/x-icon")]
    [InlineData("/apple-touch-icon.png", "image/png")]
    public async Task Icons_AreServed(string path, string contentType)
    {
        var response = await factory.CreateClient().GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(contentType, response.Content.Headers.ContentType?.MediaType);
        Assert.True((await response.Content.ReadAsByteArrayAsync()).Length > 1000);
    }
}
