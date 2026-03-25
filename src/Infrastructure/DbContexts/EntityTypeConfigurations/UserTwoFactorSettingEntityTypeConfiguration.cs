using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MonkoraEdge.Core.Auth.Infrastructure.DbContexts.EntityTypeConfigurations;

public class UserTwoFactorSettingEntityTypeConfiguration : IEntityTypeConfiguration<UserTwoFactorSetting>
{
    public void Configure(EntityTypeBuilder<UserTwoFactorSetting> builder)
    {
        builder.ToTable("tx_user_two_factor_settings");
        builder.HasKey(m => m.Id);
    }
}
