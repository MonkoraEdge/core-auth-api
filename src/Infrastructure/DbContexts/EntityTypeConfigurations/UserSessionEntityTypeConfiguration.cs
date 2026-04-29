using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MonkoraEdge.Core.Auth.Infrastructure.DbContexts.EntityTypeConfigurations;

public class UserSessionEntityTypeConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("tx_user_sessions");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.IpAddress).HasColumnName("ip_address");

        // Composite index for GetActiveByUserId + expiry filtering (called on every login and logout).
        builder.HasIndex(m => new { m.UserId, m.IsActive, m.ExpiresAt });
        // Separate index for ClientId queries (revoke-all-for-client).
        builder.HasIndex(m => m.ClientId);
    }
}
