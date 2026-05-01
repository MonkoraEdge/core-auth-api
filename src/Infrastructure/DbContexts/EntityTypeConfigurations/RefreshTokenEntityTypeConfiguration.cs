using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MonkoraEdge.Core.Auth.Infrastructure.DbContexts.EntityTypeConfigurations;

public class RefreshTokenEntityTypeConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("tx_authorization_refresh_tokens");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Scopes).HasColumnType("text[]");
        builder.Property(m => m.IpAddress).HasColumnName("ip_address");
        builder.Property(m => m.AbsoluteExpiresAt).HasColumnName("absolute_expires_at");

        builder.HasIndex(m => m.RefreshTokenHash).IsUnique();
        builder.HasIndex(m => m.FamilyId); // family revocation queries
        builder.HasIndex(m => m.UserId);
        builder.HasIndex(m => m.ExpiresAt);
        builder.HasIndex(m => m.AbsoluteExpiresAt); // cleanup + expiry queries
    }
}
