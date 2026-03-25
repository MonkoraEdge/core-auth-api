using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MonkoraEdge.Core.Auth.Infrastructure.DbContexts.EntityTypeConfigurations;

public class AgreementAcceptEntityTypeConfiguration : IEntityTypeConfiguration<AgreementAccept>
{
    public void Configure(EntityTypeBuilder<AgreementAccept> builder)
    {
        builder.ToTable("tx_agreement_accepts");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.IpAddress).HasColumnName("ip_address");
        builder.Property(m => m.AcceptanceMethod).HasDefaultValue("CHECKBOX");
    }
}
