using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Encurtador.Infrastructure.Persistence;

public sealed class LinkDbContextFactory : IDesignTimeDbContextFactory<LinkDbContext>
{
    public LinkDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Links")
            ?? "Data Source=encurtador.db";
        var options = new DbContextOptionsBuilder<LinkDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new LinkDbContext(options);
    }
}
