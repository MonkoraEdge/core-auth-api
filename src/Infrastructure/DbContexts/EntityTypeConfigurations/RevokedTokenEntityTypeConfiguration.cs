using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MonkoraEdge.Core.Auth.Infrastructure.DbContexts.EntityTypeConfigurations;

public class RevokedTokenEntityTypeConfiguration : IEntityTypeConfiguration<RevokedToken>
{
    public void Configure(EntityTypeBuilder<RevokedToken> builder)
    {
        builder.ToTable("tx_revoked_tokens");
        builder.HasKey(m => m.Id);

        builder.HasIndex(m => m.TokenHash).IsUnique();
        builder.HasIndex(m => m.ExpiresAt);
    }
}
