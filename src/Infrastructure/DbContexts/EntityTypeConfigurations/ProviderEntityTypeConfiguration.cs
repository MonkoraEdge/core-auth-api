using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MonkoraEdge.Core.Auth.Infrastructure.DbContexts.EntityTypeConfigurations;

public class ProviderEntityTypeConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> builder)
    {
        builder.ToTable("mt_providers");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.ProviderName).HasColumnType("jsonb");
        builder.Property(m => m.Scopes).HasColumnType("text[]");
    }
}
