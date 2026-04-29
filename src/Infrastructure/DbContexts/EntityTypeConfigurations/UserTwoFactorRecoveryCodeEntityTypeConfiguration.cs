using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MonkoraEdge.Core.Auth.Infrastructure.DbContexts.EntityTypeConfigurations;

public class UserTwoFactorRecoveryCodeEntityTypeConfiguration : IEntityTypeConfiguration<UserTwoFactorRecoveryCode>
{
    public void Configure(EntityTypeBuilder<UserTwoFactorRecoveryCode> builder)
    {
        builder.ToTable("tx_user_two_factor_recovery_codes");
        builder.HasKey(m => m.Id);

        // Index for GetActiveByUserIdAsync (called on every 2FA recovery code login attempt).
        builder.HasIndex(m => m.UserId);
    }
}
