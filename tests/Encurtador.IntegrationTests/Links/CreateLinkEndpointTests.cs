using System.Net;
using System.Net.Http.Json;
using Encurtador.Api.Endpoints;
using Encurtador.IntegrationTests.Links;

namespace Encurtador.IntegrationTests;

public sealed class CreateLinkEndpointTests
{
    [Fact]
    public async Task PostLinks_CreatesCustomCode()
    {
        using var factory = new LinkApiFactory();
        using var client = factory.CreateClient();
        await factory.ApplyMigrationsAsync();

        var response = await client.PostAsJsonAsync(
            "/api/links",
            new CreateLinkRequest("https://example.com/path", "Custom42"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateLinkResponse>();
        Assert.NotNull(body);
        Assert.Equal("Custom42", body.Code);
        Assert.Equal("http://localhost:5000/Custom42", body.ShortUrl);
    }

    [Fact]
    public async Task PostLinks_TreatsCustomCodesAsCaseSensitive()
    {
        using var factory = new LinkApiFactory();
        using var client = factory.CreateClient();
        await factory.ApplyMigrationsAsync();

        var upperResponse = await client.PostAsJsonAsync(
            "/api/links",
            new CreateLinkRequest("https://upper.example", "CaseCode"));
        var lowerResponse = await client.PostAsJsonAsync(
            "/api/links",
            new CreateLinkRequest("https://lower.example", "casecode"));

        Assert.Equal(HttpStatusCode.Created, upperResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, lowerResponse.StatusCode);
    }

    [Fact]
    public async Task PostLinks_GeneratesSevenCharacterUnambiguousCode()
    {
        using var factory = new LinkApiFactory();
        using var client = factory.CreateClient();
        await factory.ApplyMigrationsAsync();

        var response = await client.PostAsJsonAsync(
            "/api/links",
            new CreateLinkRequest("https://example.com", null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateLinkResponse>();
        Assert.NotNull(body);
        Assert.Equal(7, body.Code.Length);
        Assert.All(body.Code, character => Assert.Contains(character, "23456789ABCDEFGHJKMNPQRSTVWXYZ"));
    }

    [Theory]
    [InlineData("ftp://example.com", null)]
    [InlineData("https://example.com", "abc")]
    [InlineData("https://example.com", "bad-code")]
    public async Task PostLinks_RejectsInvalidInputWithProblemDetails(string destination, string? code)
    {
        using var factory = new LinkApiFactory();
        using var client = factory.CreateClient();
        await factory.ApplyMigrationsAsync();

        var response = await client.PostAsJsonAsync(
            "/api/links",
            new CreateLinkRequest(destination, code));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PostLinks_RejectsDuplicateCustomCode()
    {
        using var factory = new LinkApiFactory();
        using var client = factory.CreateClient();
        await factory.ApplyMigrationsAsync();
        var request = new CreateLinkRequest("https://example.com", "Custom42");
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/links", request)).StatusCode);

        var response = await client.PostAsJsonAsync(
            "/api/links",
            new CreateLinkRequest("https://another.example", "Custom42"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
