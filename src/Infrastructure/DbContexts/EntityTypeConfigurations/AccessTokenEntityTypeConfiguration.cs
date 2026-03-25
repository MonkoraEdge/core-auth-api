using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MonkoraEdge.Core.Auth.Infrastructure.DbContexts.EntityTypeConfigurations;

public class AccessTokenEntityTypeConfiguration : IEntityTypeConfiguration<AccessToken>
{
    public void Configure(EntityTypeBuilder<AccessToken> builder)
    {
        builder.ToTable("tx_authorization_access_tokens");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Scopes).HasColumnType("text[]");
        builder.Property(m => m.IpAddress).HasColumnName("ip_address");
        builder.Property(m => m.TokenType).HasDefaultValue("Bearer");

        // Token lookups must always use the hash, never the raw value
        builder.HasIndex(m => m.TokenHash).IsUnique();
        builder.HasIndex(m => m.UserId);
        builder.HasIndex(m => m.ExpiresAt); // for cleanup queries
    }
}
