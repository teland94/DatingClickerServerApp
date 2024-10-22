using DatingClickerServerApp.Common.Model;
using Microsoft.EntityFrameworkCore;

namespace DatingClickerServerApp.Common.Persistence
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<DatingUser> DatingUsers { get; set; }
        public DbSet<DatingUserAction> DatingUserActions { get; set; }
        public DbSet<BlacklistedDatingUser> BlacklistedDatingUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new DatingUserConfiguration());
            modelBuilder.ApplyConfiguration(new DatingUserActionConfiguration());
            modelBuilder.ApplyConfiguration(new  BlacklistedDatingUserConfiguration());
        }
    }
}
