using Encurtador.Application.Links;
using Encurtador.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encurtador.IntegrationTests.Links;

public sealed class LinkApiFactory(string? databasePath = null) : WebApplicationFactory<Program>
{
    private readonly bool deleteDatabaseOnDispose = databasePath is null;
    private readonly string databaseFile = databasePath ?? Path.Combine(
        Path.GetTempPath(),
        $"encurtador-{Guid.NewGuid():N}.db");

    public string DatabasePath => databaseFile;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Links"] = $"Data Source={databaseFile};Default Timeout=30",
                ["PublicBaseUrl"] = "http://localhost:5000"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<LinkDbContext>>();
            services.RemoveAll<LinkDbContext>();
            services.AddDbContext<LinkDbContext>(options => options.UseSqlite(
                $"Data Source={databaseFile};Default Timeout=30"));
        });
    }

    public async Task ApplyMigrationsAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LinkDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task SeedAsync(params Link[] links)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LinkDbContext>();
        dbContext.Links.AddRange(links);
        await dbContext.SaveChangesAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (deleteDatabaseOnDispose && File.Exists(databaseFile))
            {
                File.Delete(databaseFile);
            }
        }
    }
}
