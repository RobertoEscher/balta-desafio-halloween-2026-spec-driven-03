namespace Encurtador.Application.Links;

public sealed class LinkService(
    ILinkRepository repository,
    ILinkCodeGenerator codeGenerator)
{
    private const int MaxGeneratedCodeAttempts = 5;

    public async Task<CreateLinkResult> CreateAsync(
        string? destinationUrl,
        string? customCode,
        CancellationToken cancellationToken)
    {
        if (!IsValidDestination(destinationUrl))
        {
            return new CreateLinkResult(CreateLinkStatus.InvalidDestination);
        }

        if (customCode is not null && !IsValidCustomCode(customCode))
        {
            return new CreateLinkResult(CreateLinkStatus.InvalidCustomCode);
        }

        if (customCode is not null)
        {
            if (await repository.CodeExistsAsync(customCode, cancellationToken))
            {
                return new CreateLinkResult(CreateLinkStatus.CodeAlreadyInUse);
            }

            var customLink = new Link(customCode, destinationUrl!);
            var created = await repository.TryCreateAsync(customLink, cancellationToken);
            return created
                ? new CreateLinkResult(CreateLinkStatus.Created, customLink)
                : new CreateLinkResult(CreateLinkStatus.CodeAlreadyInUse);
        }

        for (var attempt = 0; attempt < MaxGeneratedCodeAttempts; attempt++)
        {
            var code = codeGenerator.Generate();
            if (await repository.CodeExistsAsync(code, cancellationToken))
            {
                continue;
            }

            var generatedLink = new Link(code, destinationUrl!);
            if (await repository.TryCreateAsync(generatedLink, cancellationToken))
            {
                return new CreateLinkResult(CreateLinkStatus.Created, generatedLink);
            }
        }

        return new CreateLinkResult(CreateLinkStatus.GeneratedCodeUnavailable);
    }

    public async Task<RedirectLinkResult> GetRedirectAsync(
        string code,
        CancellationToken cancellationToken)
    {
        var destination = await repository.FindDestinationAsync(code, cancellationToken);
        if (destination is null)
        {
            return new RedirectLinkResult(null);
        }

        if (!await repository.IncrementClickCountAsync(code, cancellationToken))
        {
            return new RedirectLinkResult(null);
        }

        return new RedirectLinkResult(destination);
    }

    public async Task<LinkStatsResult> GetStatsAsync(
        string code,
        CancellationToken cancellationToken)
    {
        var clickCount = await repository.GetClickCountAsync(code, cancellationToken);
        return clickCount.HasValue
            ? new LinkStatsResult(code, clickCount.Value)
            : new LinkStatsResult(null, null);
    }

    public static bool IsValidDestination(string? destinationUrl)
    {
        if (string.IsNullOrWhiteSpace(destinationUrl) || destinationUrl.Length > 2048)
        {
            return false;
        }

        return Uri.TryCreate(destinationUrl, UriKind.Absolute, out var uri)
            && (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            && !string.IsNullOrEmpty(uri.Host);
    }

    public static bool IsValidCustomCode(string customCode)
    {
        return customCode.Length is >= 4 and <= 20
            && customCode.All(char.IsAsciiLetterOrDigit);
    }
}

public enum CreateLinkStatus
{
    Created,
    InvalidDestination,
    InvalidCustomCode,
    CodeAlreadyInUse,
    GeneratedCodeUnavailable
}

public sealed record CreateLinkResult(CreateLinkStatus Status, Link? Link = null);

public sealed record RedirectLinkResult(string? DestinationUrl);

public sealed record LinkStatsResult(string? Code, long? ClickCount);
