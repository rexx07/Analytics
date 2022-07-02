using Analytics.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Analytics.Api.Data;

public class AnalyticsDbContext : DbContext
{
    public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : base(options)
    {
    }

    public DbSet<ArticleMatrix> ArticleMatrices { get; set; }
    /*public DbSet<Author> Authors { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Feed> Feeds { get; set; }*/

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
    }
}