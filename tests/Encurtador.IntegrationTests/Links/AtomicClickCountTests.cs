using Encurtador.Application.Links;
using Encurtador.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Encurtador.IntegrationTests.Links;

public sealed class AtomicClickCountTests
{
    [Fact]
    public async Task IncrementClickCountAsync_IncrementsAndReportsMissingCode()
    {
        using var factory = new LinkApiFactory();
        using var client = factory.CreateClient();
        await factory.ApplyMigrationsAsync();
        await factory.SeedAsync(new Link("ABC2345", "https://example.com"));

        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ILinkRepository>();

        Assert.True(await repository.IncrementClickCountAsync("ABC2345", CancellationToken.None));
        Assert.False(await repository.IncrementClickCountAsync("MISSING", CancellationToken.None));
        Assert.Equal(1, await repository.GetClickCountAsync("ABC2345", CancellationToken.None));
    }
}
