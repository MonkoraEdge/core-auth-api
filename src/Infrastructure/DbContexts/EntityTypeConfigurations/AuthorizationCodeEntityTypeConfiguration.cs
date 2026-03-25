using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MonkoraEdge.Core.Auth.Infrastructure.DbContexts.EntityTypeConfigurations;

public class AuthorizationCodeEntityTypeConfiguration : IEntityTypeConfiguration<AuthorizationCode>
{
    public void Configure(EntityTypeBuilder<AuthorizationCode> builder)
    {
        builder.ToTable("tx_authorization_codes");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Scopes).HasColumnType("text[]");

        builder.HasIndex(m => m.CodeHash).IsUnique();
        builder.HasIndex(m => m.ExpiresAt); // for cleanup queries
    }
}
