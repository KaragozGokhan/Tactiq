using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TactiqAPI.Models;

namespace TactiqAPI.Configurations;

public class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.Property(player => player.Playstyles).HasColumnName("playstyles");
        builder.Property(player => player.Pace).HasColumnName("pace");
        builder.Property(player => player.Shoot).HasColumnName("shoot");
        builder.Property(player => player.Pass).HasColumnName("pass");
        builder.Property(player => player.Dribbling).HasColumnName("dribbling");
        builder.Property(player => player.Def).HasColumnName("def");
        builder.Property(player => player.Phy).HasColumnName("phy");

        builder.HasMany(player => player.MatchPlayers)
            .WithOne(matchPlayer => matchPlayer.Player)
            .HasForeignKey(matchPlayer => matchPlayer.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(player => player.Statistics)
            .WithOne(stats => stats.Player)
            .HasForeignKey(stats => stats.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
