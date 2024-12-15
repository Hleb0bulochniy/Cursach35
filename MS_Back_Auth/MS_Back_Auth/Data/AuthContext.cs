using Microsoft.EntityFrameworkCore;
using MS_Back_Auth.Models;

namespace MS_Back_Auth.Data
{
    public class AuthContext: DbContext
    {
        public DbSet<User> Users { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.PlayerId).IsUnique();
                entity.HasIndex(u => u.CreatorId).IsUnique();
            });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Data Source=MSI\SQLEXPRESS;Initial Catalog=MS_Auth;Persist Security Info=True;User ID=daniil;Password=test;Trust Server Certificate=True");
        }
    }
}
