using MatchBetting.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MatchBetting.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Models.MatchBetting> MatchBettings { get; set; }
        public DbSet<Models.Match> Matches { get; set; }
        public DbSet<Models.SideBet> SideBettings { get; set; }
        public DbSet<Models.Log> Logs { get; set; }
        public DbSet<FootballPlayers> FootballPlayers { get; set; }

    }
}
