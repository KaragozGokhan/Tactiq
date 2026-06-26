using System.Reflection;
using Microsoft.EntityFrameworkCore;
using TactiqAPI.Models;

namespace TactiqAPI.Data;

public class TactiqDbContext : DbContext
{
    public TactiqDbContext(DbContextOptions<TactiqDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<MatchPlayer> MatchPlayers => Set<MatchPlayer>();
    public DbSet<PlayerStats> PlayerStats => Set<PlayerStats>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
