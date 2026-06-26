using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TactiqAPI.Models;

namespace TactiqAPI.Configurations;

public class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.HasMany(match => match.MatchPlayers)
            .WithOne(matchPlayer => matchPlayer.Match)
            .HasForeignKey(matchPlayer => matchPlayer.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(match => match.Statistics)
            .WithOne(stats => stats.Match)
            .HasForeignKey(stats => stats.MatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
