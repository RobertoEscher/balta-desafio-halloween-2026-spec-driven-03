using Encurtador.Application.Links;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Encurtador.Infrastructure.Persistence;

public sealed class LinkRepository(LinkDbContext dbContext) : ILinkRepository
{
    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken) =>
        dbContext.Links.AnyAsync(link => link.Code == code, cancellationToken);

    public async Task<bool> TryCreateAsync(Link link, CancellationToken cancellationToken)
    {
        dbContext.Links.Add(link);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (IsCodeUniqueViolation(exception))
        {
            dbContext.Entry(link).State = EntityState.Detached;
            return false;
        }
    }

    public Task<string?> FindDestinationAsync(string code, CancellationToken cancellationToken) =>
        dbContext.Links
            .AsNoTracking()
            .Where(link => link.Code == code)
            .Select(link => link.DestinationUrl)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<bool> IncrementClickCountAsync(string code, CancellationToken cancellationToken)
    {
        var affectedRows = await dbContext.Links
            .Where(link => link.Code == code)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(link => link.ClickCount, link => link.ClickCount + 1),
                cancellationToken);

        return affectedRows > 0;
    }

    public Task<long?> GetClickCountAsync(string code, CancellationToken cancellationToken) =>
        dbContext.Links
            .AsNoTracking()
            .Where(link => link.Code == code)
            .Select(link => (long?)link.ClickCount)
            .SingleOrDefaultAsync(cancellationToken);

    private static bool IsCodeUniqueViolation(DbUpdateException exception)
    {
        var sqliteException = exception.InnerException as SqliteException;
        return sqliteException is
            {
                SqliteErrorCode: 19,
                SqliteExtendedErrorCode: 2067
            }
            && sqliteException.Message.Contains("Links.Code", StringComparison.Ordinal);
    }
}
