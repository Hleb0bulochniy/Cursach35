using Microsoft.EntityFrameworkCore;
using MS_Back_Auth.Models;

namespace MS_Back_Auth.Data
{
    public class AuthContext: DbContext
    {
        public DbSet<User> Users { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Data Source=MSI\SQLEXPRESS;Initial Catalog=MS_Auth;Persist Security Info=True;User ID=daniil;Password=test;Trust Server Certificate=True");
        }
    }
}
