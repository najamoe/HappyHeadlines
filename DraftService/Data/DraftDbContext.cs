using Microsoft.EntityFrameworkCore;
using DraftService.Models;

namespace DraftService.Data
{
    public class DraftDbContext : DbContext
    {
        public DraftDbContext(DbContextOptions<DraftDbContext> options) : base(options) { }
        public DbSet<Draft> Drafts { get; set; }
    }
}
