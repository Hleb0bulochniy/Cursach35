using Microsoft.EntityFrameworkCore;
using MS_Back_Logs.Models;

namespace MS_Back_Logs.Data
{
    public class LogsContext : DbContext
    {
        public DbSet<Log> Logs { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Data Source=MSI\SQLEXPRESS;Initial Catalog=MS_Auth;Persist Security Info=True;User ID=daniil;Password=test;Trust Server Certificate=True");
        }
    }
}
