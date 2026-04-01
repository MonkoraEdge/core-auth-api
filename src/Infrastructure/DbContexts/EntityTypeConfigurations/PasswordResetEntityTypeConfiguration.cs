using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MonkoraEdge.Core.Auth.Infrastructure.DbContexts.EntityTypeConfigurations;

public class PasswordResetEntityTypeConfiguration : IEntityTypeConfiguration<PasswordReset>
{
    public void Configure(EntityTypeBuilder<PasswordReset> builder)
    {
        builder.ToTable("tx_password_resets");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.IpAddress).HasColumnName("ip_address");

        builder.HasIndex(m => m.TokenHash).IsUnique(); // GetByTokenHashAsync lookup
        builder.HasIndex(m => m.UserId);               // GetByUserIdAsync + TryConsumeAsync
    }
}
