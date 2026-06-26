using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TactiqAPI.Models;

namespace TactiqAPI.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasMany(user => user.Players)
            .WithOne(player => player.CreatedByUser)
            .HasForeignKey(player => player.CreatedByUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(user => user.Matches)
            .WithOne(match => match.CreatedByUser)
            .HasForeignKey(match => match.CreatedByUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
