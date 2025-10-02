using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SubscriberService.Models;

namespace SubscriberService.Data
{
    public class SubscriberServiceContext : DbContext
    {
        public SubscriberServiceContext (DbContextOptions<SubscriberServiceContext> options)
            : base(options)
        {
        }

        public DbSet<SubscriberService.Models.Subscriber> Subscriber { get; set; } = default!;
    }
}
