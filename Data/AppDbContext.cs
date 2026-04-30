using Microsoft.EntityFrameworkCore;
using RecommendationApp.Models;

namespace RecommendationApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<Behavior> Behaviors { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Rating>()
                .HasOne(r => r.User).WithMany(u => u.Ratings).HasForeignKey(r => r.UserId);
            modelBuilder.Entity<Rating>()
                .HasOne(r => r.Product).WithMany(p => p.Ratings).HasForeignKey(r => r.ProductId);

            modelBuilder.Entity<Behavior>()
                .HasOne(b => b.User).WithMany(u => u.Behaviors).HasForeignKey(b => b.UserId);
            modelBuilder.Entity<Behavior>()
                .HasOne(b => b.Product).WithMany(p => p.Behaviors).HasForeignKey(b => b.ProductId);
        }
    }
}
