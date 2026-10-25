using Encurtador.Application.Links;

namespace Encurtador.UnitTests.Links;

public sealed class LinkServiceCollisionTests
{
    [Fact]
    public async Task CreateAsync_ReportsCustomCollisionFromAtomicInsert()
    {
        var repository = new FakeLinkRepository { RejectNextInsert = true };
        var service = new LinkService(repository, new SequenceCodeGenerator(["AB23456"]));

        var result = await service.CreateAsync("https://example.com", "Custom42", CancellationToken.None);

        Assert.Equal(CreateLinkStatus.CodeAlreadyInUse, result.Status);
        Assert.Empty(repository.Links);
    }

    [Fact]
    public async Task CreateAsync_RetriesGeneratedCollision()
    {
        var repository = new FakeLinkRepository { RejectNextInsert = true };
        var generator = new SequenceCodeGenerator(["AB23456", "CD23456"]);
        var service = new LinkService(repository, generator);

        var result = await service.CreateAsync("https://example.com", null, CancellationToken.None);

        Assert.Equal(CreateLinkStatus.Created, result.Status);
        Assert.Equal("CD23456", result.Link!.Code);
        Assert.Equal(2, generator.Calls);
    }

    [Fact]
    public async Task CreateAsync_StopsAfterFiveGeneratedCodeCollisions()
    {
        var repository = new FakeLinkRepository { RejectAllInserts = true };
        var generator = new SequenceCodeGenerator(["AAAAAAA", "BBBBBBB", "CCCCCCC", "DDDDDDD", "EEEEEEE"]);
        var service = new LinkService(repository, generator);

        var result = await service.CreateAsync("https://example.com", null, CancellationToken.None);

        Assert.Equal(CreateLinkStatus.GeneratedCodeUnavailable, result.Status);
        Assert.Equal(5, generator.Calls);
    }

    private sealed class SequenceCodeGenerator(IEnumerable<string> codes) : ILinkCodeGenerator
    {
        private readonly Queue<string> codes = new(codes);

        public int Calls { get; private set; }

        public string Generate()
        {
            Calls++;
            return codes.Dequeue();
        }
    }

    private sealed class FakeLinkRepository : ILinkRepository
    {
        public Dictionary<string, Link> Links { get; } = new(StringComparer.Ordinal);

        public bool RejectNextInsert { get; init; }

        public bool RejectAllInserts { get; init; }

        private int insertCalls;

        public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult(Links.ContainsKey(code));

        public Task<bool> TryCreateAsync(Link link, CancellationToken cancellationToken)
        {
            insertCalls++;
            if (RejectAllInserts || (RejectNextInsert && insertCalls == 1))
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(Links.TryAdd(link.Code, link));
        }

        public Task<string?> FindDestinationAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult<string?>(Links.TryGetValue(code, out var link) ? link.DestinationUrl : null);

        public Task<bool> IncrementClickCountAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult(Links.ContainsKey(code));

        public Task<long?> GetClickCountAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult<long?>(Links.TryGetValue(code, out var link) ? link.ClickCount : null);
    }
}
