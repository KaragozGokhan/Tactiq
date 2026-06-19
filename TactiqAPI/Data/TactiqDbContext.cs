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

        // User configuration
        modelBuilder.Entity<User>()
            .HasKey(u => u.Id);
        modelBuilder.Entity<User>()
            .HasMany(u => u.Players)
            .WithOne(p => p.CreatedByUser)
            .HasForeignKey(p => p.CreatedByUserId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<User>()
            .HasMany(u => u.Matches)
            .WithOne(m => m.CreatedByUser)
            .HasForeignKey(m => m.CreatedByUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Player configuration
        modelBuilder.Entity<Player>()
            .HasKey(p => p.Id);
        modelBuilder.Entity<Player>()
            .HasMany(p => p.MatchPlayers)
            .WithOne(mp => mp.Player)
            .HasForeignKey(mp => mp.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Player>()
            .HasMany(p => p.Statistics)
            .WithOne(ps => ps.Player)
            .HasForeignKey(ps => ps.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Match configuration
        modelBuilder.Entity<Match>()
            .HasKey(m => m.Id);
        modelBuilder.Entity<Match>()
            .HasMany(m => m.MatchPlayers)
            .WithOne(mp => mp.Match)
            .HasForeignKey(mp => mp.MatchId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Match>()
            .HasMany(m => m.Statistics)
            .WithOne(ps => ps.Match)
            .HasForeignKey(ps => ps.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        // MatchPlayer configuration
        modelBuilder.Entity<MatchPlayer>()
            .HasKey(mp => mp.Id);

        // PlayerStats configuration
        modelBuilder.Entity<PlayerStats>()
            .HasKey(ps => ps.Id);
    }
}
