using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MonkoraEdge.Core.Auth.Infrastructure.DbContexts.EntityTypeConfigurations;

public class UserRoleEntityTypeConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("lnk_user_roles");
        builder.HasKey(m => m.Id);

        // Enforce uniqueness and support efficient lookups for both FK directions.
        builder.HasIndex(m => new { m.UserId, m.RoleId }).IsUnique();
        builder.HasIndex(m => m.UserId);
        builder.HasIndex(m => m.RoleId);
    }
}
