using BBallStats.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BBallStats.Data
{
    public class ForumDbContext : DbContext
    {
        private readonly IConfiguration _configuration;
        public DbSet<Team> Teams { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<PlayerStatistic> PlayerStatistics { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<RatingAlgorithm> RatingAlgorithms { get; set; }
        public DbSet<AlgorithmStatistic> AlgorithmStatistics { get; set; }
        public DbSet<Statistic> Statistics { get; set; }

        public ForumDbContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(_configuration.GetConnectionString("PostgreSQL"));
        }
    }
}
