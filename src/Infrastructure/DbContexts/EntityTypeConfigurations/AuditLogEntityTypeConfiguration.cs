using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MonkoraEdge.Core.Auth.Infrastructure.DbContexts.EntityTypeConfigurations;

public class AuditLogEntityTypeConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Metadata).HasColumnType("jsonb");
        builder.Property(m => m.IpAddress).HasColumnName("ip_address");
    }
}
