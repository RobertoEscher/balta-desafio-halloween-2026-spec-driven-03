using Encurtador.Application.Links;
namespace Encurtador.Api.Endpoints;

public static class LinksEndpoints
{
    public static IEndpointRouteBuilder MapLinksEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/links");
        group.MapPost("", CreateAsync);
        group.MapGet("/{code}/stats", GetStatsAsync);
        endpoints.MapGet("/{code}", RedirectAsync);
        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        CreateLinkRequest request,
        LinkService linkService,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var result = await linkService.CreateAsync(
            request.DestinationUrl,
            request.CustomCode,
            cancellationToken);

        return result.Status switch
        {
            CreateLinkStatus.Created => Created(result.Link!, configuration),
            CreateLinkStatus.InvalidDestination => Problem(
                StatusCodes.Status400BadRequest,
                "Invalid destination URL",
                "Destination URL must be an absolute HTTP or HTTPS URL with at most 2048 characters."),
            CreateLinkStatus.InvalidCustomCode => Problem(
                StatusCodes.Status400BadRequest,
                "Invalid custom code",
                "Custom codes must contain 4 to 20 ASCII letters or digits."),
            CreateLinkStatus.CodeAlreadyInUse => Problem(
                StatusCodes.Status409Conflict,
                "Code already in use",
                "The requested custom code is already associated with a link."),
            CreateLinkStatus.GeneratedCodeUnavailable => Problem(
                StatusCodes.Status503ServiceUnavailable,
                "Could not allocate a short code",
                "A unique generated code could not be allocated after five attempts."),
            _ => throw new InvalidOperationException($"Unexpected link creation status: {result.Status}.")
        };
    }

    private static async Task<IResult> RedirectAsync(
        string code,
        LinkService linkService,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var result = await linkService.GetRedirectAsync(code, cancellationToken);
        if (result.DestinationUrl is null)
        {
            return Problem(
                StatusCodes.Status404NotFound,
                "Link not found",
                "No link exists for the supplied code.");
        }

        context.Response.Headers.CacheControl = "no-store";
        return Results.Redirect(result.DestinationUrl, permanent: false);
    }

    private static async Task<IResult> GetStatsAsync(
        string code,
        LinkService linkService,
        CancellationToken cancellationToken)
    {
        var result = await linkService.GetStatsAsync(code, cancellationToken);
        if (!result.ClickCount.HasValue)
        {
            return Problem(
                StatusCodes.Status404NotFound,
                "Link not found",
                "No link exists for the supplied code.");
        }

        return Results.Ok(new LinkStatsResponse(result.Code!, result.ClickCount.Value));
    }

    private static IResult Created(Link link, IConfiguration configuration)
    {
        var publicBaseUrl = configuration["PublicBaseUrl"];
        if (!Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var baseUri)
            || (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("PublicBaseUrl must be configured as an absolute HTTP or HTTPS URL.");
        }

        var normalizedBaseUri = new Uri(baseUri.ToString().TrimEnd('/') + "/", UriKind.Absolute);
        var shortUrl = new Uri(normalizedBaseUri, Uri.EscapeDataString(link.Code)).AbsoluteUri;
        return Results.Created(shortUrl, new CreateLinkResponse(link.Code, shortUrl));
    }

    private static IResult Problem(int statusCode, string title, string detail) =>
        Results.Problem(statusCode: statusCode, title: title, detail: detail);
}

public sealed record CreateLinkRequest(string? DestinationUrl, string? CustomCode);

public sealed record CreateLinkResponse(string Code, string ShortUrl);

public sealed record LinkStatsResponse(string Code, long ClickCount);
