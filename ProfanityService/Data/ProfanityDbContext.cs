using Microsoft.EntityFrameworkCore;
using ProfanityService.Models;

namespace ProfanityService.Data
{
    public class ProfanityDbContext : DbContext
    {
        public ProfanityDbContext(DbContextOptions<ProfanityDbContext> options) : base(options) { }
        public DbSet<Profanity> Profanities { get; set; }
    }
}
