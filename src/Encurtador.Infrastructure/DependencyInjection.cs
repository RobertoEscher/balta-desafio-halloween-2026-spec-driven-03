using Encurtador.Application.Links;
using Encurtador.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Encurtador.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Links");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:Links must be configured.");
        }

        services.AddDbContext<LinkDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<ILinkRepository, LinkRepository>();
        return services;
    }
}
