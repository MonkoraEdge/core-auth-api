using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MonkoraEdge.Core.Auth.Infrastructure.DbContexts.EntityTypeConfigurations;

public class UserSessionDeviceEntityTypeConfiguration : IEntityTypeConfiguration<UserSessionDevice>
{
    public void Configure(EntityTypeBuilder<UserSessionDevice> builder)
    {
        builder.ToTable("tx_user_sessions_devices");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.DeviceType).HasDefaultValue("UNKNOWN");
        builder.Property(m => m.IpAddress).HasColumnName("ip_address");

        // Index for GetByUserIdAsync (device management endpoints).
        builder.HasIndex(m => m.UserId);
    }
}
