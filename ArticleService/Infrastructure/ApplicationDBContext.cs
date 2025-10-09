using ArticleService.Models;
using Microsoft.EntityFrameworkCore;

namespace ArticleService.Infrastructure
{
    public class AfricaDbContext : DbContext
    {
        public AfricaDbContext(DbContextOptions<AfricaDbContext> options) : base(options) { }
        public DbSet<Article> Articles { get; set; }
    }

    public class AsiaDbContext : DbContext
    {
        public AsiaDbContext(DbContextOptions<AsiaDbContext> options) : base(options) { }
        public DbSet<Article> Articles { get; set; }
    }

    public class EuropeDbContext : DbContext
    {
        public EuropeDbContext(DbContextOptions<EuropeDbContext> options) : base(options) { }
        public DbSet<Article> Articles { get; set; }
    }

    public class NorthAmericaDbContext : DbContext
    {
        public NorthAmericaDbContext(DbContextOptions<NorthAmericaDbContext> options) : base(options) { }
        public DbSet<Article> Articles { get; set; }
    }

    public class SouthAmericaDbContext : DbContext
    {
        public SouthAmericaDbContext(DbContextOptions<SouthAmericaDbContext> options) : base(options) { }
        public DbSet<Article> Articles { get; set; }
    }

    public class OceaniaDbContext : DbContext
    {
        public OceaniaDbContext(DbContextOptions<OceaniaDbContext> options) : base(options) { }
        public DbSet<Article> Articles { get; set; }
    }

    public class AntarcticaDbContext : DbContext
    {
        public AntarcticaDbContext(DbContextOptions<AntarcticaDbContext> options) : base(options) { }
        public DbSet<Article> Articles { get; set; }
    }

    public class GlobalDbContext : DbContext
    {
        public GlobalDbContext(DbContextOptions<GlobalDbContext> options) : base(options) { }
        public DbSet<GlobalArticle> Articles { get; set; }
    }
}
