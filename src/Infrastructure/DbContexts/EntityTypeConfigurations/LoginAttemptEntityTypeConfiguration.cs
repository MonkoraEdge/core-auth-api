using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MonkoraEdge.Core.Auth.Infrastructure.DbContexts.EntityTypeConfigurations;

public class LoginAttemptEntityTypeConfiguration : IEntityTypeConfiguration<LoginAttempt>
{
    public void Configure(EntityTypeBuilder<LoginAttempt> builder)
    {
        builder.ToTable("tx_login_attempts");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.IpAddress).HasColumnName("ip_address");

        // Composite index for rate-limit queries: find recent failed attempts by IP
        builder.HasIndex(m => new { m.IpAddress, m.Success, m.CreatedAt });
    }
}
