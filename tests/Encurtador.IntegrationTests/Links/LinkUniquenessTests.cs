using Encurtador.Application.Links;
using Encurtador.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Encurtador.IntegrationTests.Links;

public sealed class LinkUniquenessTests
{
    [Fact]
    public async Task TryCreateAsync_RejectsDuplicateCodeAtDatabaseConstraint()
    {
        using var factory = new LinkApiFactory();
        using var client = factory.CreateClient();
        await factory.ApplyMigrationsAsync();

        using var firstScope = factory.Services.CreateScope();
        var firstRepository = firstScope.ServiceProvider.GetRequiredService<ILinkRepository>();
        Assert.True(await firstRepository.TryCreateAsync(
            new Link("Custom42", "https://first.example"),
            CancellationToken.None));

        using var secondScope = factory.Services.CreateScope();
        var secondRepository = secondScope.ServiceProvider.GetRequiredService<ILinkRepository>();
        var secondResult = await secondRepository.TryCreateAsync(
            new Link("Custom42", "https://second.example"),
            CancellationToken.None);

        Assert.False(secondResult);
        using var verificationScope = factory.Services.CreateScope();
        var dbContext = verificationScope.ServiceProvider.GetRequiredService<LinkDbContext>();
        Assert.Equal("https://first.example", await dbContext.Links
            .Where(link => link.Code == "Custom42")
            .Select(link => link.DestinationUrl)
            .SingleAsync());
    }
}
