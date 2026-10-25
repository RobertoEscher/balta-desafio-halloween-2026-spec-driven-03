namespace Encurtador.Application.Links;

public interface ILinkRepository
{
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken);

    Task<bool> TryCreateAsync(Link link, CancellationToken cancellationToken);

    Task<string?> FindDestinationAsync(string code, CancellationToken cancellationToken);

    Task<bool> IncrementClickCountAsync(string code, CancellationToken cancellationToken);

    Task<long?> GetClickCountAsync(string code, CancellationToken cancellationToken);
}
