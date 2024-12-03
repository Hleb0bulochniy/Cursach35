using Microsoft.EntityFrameworkCore;
using MS_Back_Maps.Models;

namespace MS_Back_Maps.Data
{
    public class MapsContext : DbContext
    {
        public DbSet<Map> Maps { get; set; } = null!;
        public DbSet<MapsInUser> MapsInUsers { get; set; } = null!;
        public DbSet<CustomMap> CustomMaps { get; set; } = null!;
        public DbSet<CustomMapsInUser> CustomMapsInUsers { get; set;} = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Data Source=MSI\SQLEXPRESS;Initial Catalog=MS_Auth;Persist Security Info=True;User ID=daniil;Password=test;Trust Server Certificate=True");
        }
    }
}
