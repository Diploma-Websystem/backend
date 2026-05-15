using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebUtilities.Core.Entities;

namespace WebUtilities.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<UrlRecord> UrlRecords => Set<UrlRecord>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UrlRecord>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OriginalUrl).IsRequired().HasMaxLength(2048);
            entity.Property(x => x.ShortCode).IsRequired().HasMaxLength(12);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.HasIndex(x => x.ShortCode).IsUnique();
        });
    }
}
