using Microsoft.EntityFrameworkCore;
using SubscriberService.Models;

namespace SubscriberService.Data
{
    public class SubscriberDBContext : DbContext
    {
        public SubscriberDBContext(DbContextOptions<SubscriberDBContext> options)
            : base(options)
        {
        }

        // DbSet for your entity
        public DbSet<Subscriber> Subscribers { get; set; } = default!;
    }
}
