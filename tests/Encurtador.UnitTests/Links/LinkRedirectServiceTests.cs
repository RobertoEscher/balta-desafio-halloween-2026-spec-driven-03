using Encurtador.Application.Links;

namespace Encurtador.UnitTests.Links;

public sealed class LinkRedirectServiceTests
{
    [Fact]
    public async Task GetRedirectAsync_IncrementsAfterResolvingExistingLink()
    {
        var repository = new FakeLinkRepository();
        repository.Add(new Link("ABC2345", "https://example.com"));
        var service = new LinkService(repository, new FixedCodeGenerator());

        var result = await service.GetRedirectAsync("ABC2345", CancellationToken.None);

        Assert.Equal("https://example.com", result.DestinationUrl);
        Assert.Equal(1, repository.IncrementCalls);
    }

    [Fact]
    public async Task GetRedirectAsync_DoesNotIncrementUnknownCode()
    {
        var repository = new FakeLinkRepository();
        var service = new LinkService(repository, new FixedCodeGenerator());

        var result = await service.GetRedirectAsync("UNKNOWN", CancellationToken.None);

        Assert.Null(result.DestinationUrl);
        Assert.Equal(0, repository.IncrementCalls);
    }

    [Fact]
    public async Task GetStatsAsync_ReturnsCountWithoutIncrementing()
    {
        var repository = new FakeLinkRepository();
        repository.Add(new Link("ABC2345", "https://example.com"));
        var service = new LinkService(repository, new FixedCodeGenerator());

        var result = await service.GetStatsAsync("ABC2345", CancellationToken.None);

        Assert.Equal(0, result.ClickCount);
        Assert.Equal(0, repository.IncrementCalls);
    }

    private sealed class FixedCodeGenerator : ILinkCodeGenerator
    {
        public string Generate() => "ABC2345";
    }

    private sealed class FakeLinkRepository : ILinkRepository
    {
        private readonly Dictionary<string, (Link Link, long ClickCount)> links = new(StringComparer.Ordinal);

        public int IncrementCalls { get; private set; }

        public void Add(Link link) => links.Add(link.Code, (link, 0));

        public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult(links.ContainsKey(code));

        public Task<bool> TryCreateAsync(Link link, CancellationToken cancellationToken) =>
            Task.FromResult(links.TryAdd(link.Code, (link, 0)));

        public Task<string?> FindDestinationAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult<string?>(links.TryGetValue(code, out var entry) ? entry.Link.DestinationUrl : null);

        public Task<bool> IncrementClickCountAsync(string code, CancellationToken cancellationToken)
        {
            IncrementCalls++;
            if (!links.TryGetValue(code, out var entry))
            {
                return Task.FromResult(false);
            }

            links[code] = (entry.Link, entry.ClickCount + 1);
            return Task.FromResult(true);
        }

        public Task<long?> GetClickCountAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult<long?>(links.TryGetValue(code, out var entry) ? entry.ClickCount : null);
    }
}
