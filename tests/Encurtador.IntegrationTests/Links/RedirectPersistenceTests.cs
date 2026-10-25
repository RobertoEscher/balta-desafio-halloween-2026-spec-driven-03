using System.Net;
using System.Net.Http.Json;
using Encurtador.Api.Endpoints;
using Encurtador.Application.Links;

namespace Encurtador.IntegrationTests.Links;

public sealed class RedirectPersistenceTests
{
    [Fact]
    public async Task ConcurrentRedirects_DoNotLoseIncrements()
    {
        using var factory = new LinkApiFactory();
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await factory.ApplyMigrationsAsync();
        await factory.SeedAsync(new Link("ABC2345", "https://example.com"));

        const int requests = 12;
        var responses = await Task.WhenAll(
            Enumerable.Range(0, requests).Select(_ => client.GetAsync("/ABC2345")));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Found, response.StatusCode));
        var stats = await client.GetFromJsonAsync<LinkStatsResponse>("/api/links/ABC2345/stats");
        Assert.NotNull(stats);
        Assert.Equal(requests, stats.ClickCount);
    }

    [Fact]
    public async Task LinkAndCountRemainAfterHostRestart()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"encurtador-restart-{Guid.NewGuid():N}.db");
        try
        {
            using (var firstFactory = new LinkApiFactory(databasePath))
            using (var firstClient = firstFactory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            }))
            {
                await firstFactory.ApplyMigrationsAsync();
                await firstFactory.SeedAsync(new Link("ABC2345", "https://example.com"));
                Assert.Equal(HttpStatusCode.Found, (await firstClient.GetAsync("/ABC2345")).StatusCode);
            }

            using var restartedFactory = new LinkApiFactory(databasePath);
            using var restartedClient = restartedFactory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            await restartedFactory.ApplyMigrationsAsync();

            var response = await restartedClient.GetAsync("/ABC2345");
            var stats = await restartedClient.GetFromJsonAsync<LinkStatsResponse>("/api/links/ABC2345/stats");

            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.NotNull(stats);
            Assert.Equal(2, stats.ClickCount);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }
}
