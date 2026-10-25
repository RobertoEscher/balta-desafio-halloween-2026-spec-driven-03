using System.Net;
using System.Net.Http.Json;
using Encurtador.Api.Endpoints;
using Encurtador.Application.Links;

namespace Encurtador.IntegrationTests.Links;

public sealed class LinkStatsEndpointTests
{
    [Fact]
    public async Task GetStats_ReturnsZeroAndDoesNotIncrement()
    {
        using var factory = new LinkApiFactory();
        using var client = factory.CreateClient();
        await factory.ApplyMigrationsAsync();
        await factory.SeedAsync(new Link("ABC2345", "https://example.com"));

        var response = await client.GetAsync("/api/links/ABC2345/stats");
        var stats = await response.Content.ReadFromJsonAsync<LinkStatsResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(stats);
        Assert.Equal("ABC2345", stats.Code);
        Assert.Equal(0, stats.ClickCount);
    }

    [Fact]
    public async Task GetStats_ReturnsNotFoundForUnknownCode()
    {
        using var factory = new LinkApiFactory();
        using var client = factory.CreateClient();
        await factory.ApplyMigrationsAsync();

        var response = await client.GetAsync("/api/links/MISSING/stats");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
