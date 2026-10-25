using Encurtador.Application.Links;

namespace Encurtador.UnitTests.Links;

public sealed class LinkServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ftp://example.com")]
    [InlineData("example.com")]
    [InlineData("https://")]
    public async Task CreateAsync_RejectsInvalidDestination(string? destination)
    {
        var service = new LinkService(new InMemoryLinkRepository(), new FixedCodeGenerator("ABC2345"));

        var result = await service.CreateAsync(destination, null, CancellationToken.None);

        Assert.Equal(CreateLinkStatus.InvalidDestination, result.Status);
    }

    [Fact]
    public async Task CreateAsync_AcceptsDestinationAtMaximumLength()
    {
        var url = "https://a/" + new string('a', 2038);
        Assert.Equal(2048, url.Length);
        var service = new LinkService(new InMemoryLinkRepository(), new FixedCodeGenerator("ABC2345"));

        var result = await service.CreateAsync(url, null, CancellationToken.None);

        Assert.Equal(CreateLinkStatus.Created, result.Status);
    }

    [Fact]
    public async Task CreateAsync_RejectsDestinationAboveMaximumLength()
    {
        var url = "https://a/" + new string('a', 2039);
        var service = new LinkService(new InMemoryLinkRepository(), new FixedCodeGenerator("ABC2345"));

        var result = await service.CreateAsync(url, null, CancellationToken.None);

        Assert.Equal(CreateLinkStatus.InvalidDestination, result.Status);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("abcdefghijklmnopqrstu")]
    [InlineData("abc-123")]
    [InlineData("abcdé")]
    public async Task CreateAsync_RejectsInvalidCustomCode(string code)
    {
        var service = new LinkService(new InMemoryLinkRepository(), new FixedCodeGenerator("ABC2345"));

        var result = await service.CreateAsync("https://example.com", code, CancellationToken.None);

        Assert.Equal(CreateLinkStatus.InvalidCustomCode, result.Status);
    }

    [Fact]
    public async Task CreateAsync_AcceptsCustomCodeAtMinimumAndMaximumLengths()
    {
        var repository = new InMemoryLinkRepository();
        var service = new LinkService(repository, new FixedCodeGenerator("ABC2345"));

        var min = await service.CreateAsync("https://example.com", "A234", CancellationToken.None);
        var max = await service.CreateAsync("https://example.com", new string('a', 20), CancellationToken.None);

        Assert.Equal(CreateLinkStatus.Created, min.Status);
        Assert.Equal(CreateLinkStatus.Created, max.Status);
    }

    private sealed class FixedCodeGenerator(string code) : ILinkCodeGenerator
    {
        public string Generate() => code;
    }

    private sealed class InMemoryLinkRepository : ILinkRepository
    {
        private readonly Dictionary<string, Link> links = new(StringComparer.Ordinal);

        public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult(links.ContainsKey(code));

        public Task<bool> TryCreateAsync(Link link, CancellationToken cancellationToken)
        {
            if (!links.TryAdd(link.Code, link))
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        public Task<string?> FindDestinationAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult<string?>(links.TryGetValue(code, out var link) ? link.DestinationUrl : null);

        public Task<bool> IncrementClickCountAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult(links.ContainsKey(code));

        public Task<long?> GetClickCountAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult<long?>(links.TryGetValue(code, out var link) ? link.ClickCount : null);
    }
}
