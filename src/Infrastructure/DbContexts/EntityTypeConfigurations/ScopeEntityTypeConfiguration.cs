using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MonkoraEdge.Core.Auth.Infrastructure.DbContexts.EntityTypeConfigurations;

public class ScopeEntityTypeConfiguration : IEntityTypeConfiguration<Scope>
{
    public void Configure(EntityTypeBuilder<Scope> builder)
    {
        builder.ToTable("mt_scopes");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Claims).HasColumnType("text[]");
        builder.Property(m => m.ScopeType).HasDefaultValue("CUSTOM");
    }
}
