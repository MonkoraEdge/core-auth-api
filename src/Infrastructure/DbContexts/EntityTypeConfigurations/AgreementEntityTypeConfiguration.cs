using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MonkoraEdge.Core.Auth.Infrastructure.DbContexts.EntityTypeConfigurations;

public class AgreementEntityTypeConfiguration : IEntityTypeConfiguration<Agreement>
{
    public void Configure(EntityTypeBuilder<Agreement> builder)
    {
        builder.ToTable("mt_agreements");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Title).HasColumnType("jsonb");
        builder.Property(m => m.Content).HasColumnType("jsonb");
        builder.Property(m => m.Summary).HasColumnType("jsonb");
    }
}
