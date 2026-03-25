using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MonkoraEdge.Core.Auth.Infrastructure.DbContexts.EntityTypeConfigurations;

public class RolePermissionEntityTypeConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("lnk_role_permissions");
        builder.HasKey(m => m.Id);
    }
}
