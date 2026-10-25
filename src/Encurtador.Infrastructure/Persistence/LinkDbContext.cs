using Encurtador.Application.Links;
using Microsoft.EntityFrameworkCore;

namespace Encurtador.Infrastructure.Persistence;

public sealed class LinkDbContext(DbContextOptions<LinkDbContext> options) : DbContext(options)
{
    public DbSet<Link> Links => Set<Link>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var link = modelBuilder.Entity<Link>();
        link.ToTable("Links");
        link.HasKey(item => item.Id);
        link.Property(item => item.Code)
            .HasMaxLength(20)
            .UseCollation("BINARY")
            .IsRequired();
        link.HasIndex(item => item.Code)
            .IsUnique()
            .HasDatabaseName("UX_Links_Code");
        link.Property(item => item.DestinationUrl)
            .HasMaxLength(2048)
            .IsRequired();
        link.Property(item => item.ClickCount)
            .HasDefaultValue(0L)
            .IsRequired();
    }
}
