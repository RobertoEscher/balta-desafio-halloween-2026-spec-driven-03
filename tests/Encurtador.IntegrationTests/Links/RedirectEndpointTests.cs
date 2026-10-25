using System.Net;
using System.Net.Http.Json;
using Encurtador.Api.Endpoints;
using Encurtador.Application.Links;

namespace Encurtador.IntegrationTests.Links;

public sealed class RedirectEndpointTests
{
    [Fact]
    public async Task GetCode_RedirectsAndCountsAccess()
    {
        using var factory = new LinkApiFactory();
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await factory.ApplyMigrationsAsync();
        await factory.SeedAsync(new Link("ABC2345", "https://example.com/landing"));

        var response = await client.GetAsync("/ABC2345");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("https://example.com/landing", response.Headers.Location?.AbsoluteUri);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        var stats = await client.GetFromJsonAsync<LinkStatsResponse>("/api/links/ABC2345/stats");
        Assert.NotNull(stats);
        Assert.Equal(1, stats.ClickCount);
    }

    [Fact]
    public async Task GetUnknownCode_ReturnsNotFoundWithoutIncrementingOtherLinks()
    {
        using var factory = new LinkApiFactory();
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await factory.ApplyMigrationsAsync();
        await factory.SeedAsync(new Link("ABC2345", "https://example.com"));

        var response = await client.GetAsync("/MISSING");
        var stats = await client.GetFromJsonAsync<LinkStatsResponse>("/api/links/ABC2345/stats");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(stats);
        Assert.Equal(0, stats.ClickCount);
    }
}
