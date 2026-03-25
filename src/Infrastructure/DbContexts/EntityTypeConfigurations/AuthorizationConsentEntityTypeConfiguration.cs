using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MonkoraEdge.Core.Auth.Infrastructure.DbContexts.EntityTypeConfigurations;

public class AuthorizationConsentEntityTypeConfiguration : IEntityTypeConfiguration<AuthorizationConsent>
{
    public void Configure(EntityTypeBuilder<AuthorizationConsent> builder)
    {
        builder.ToTable("tx_authorization_consents");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Scopes).HasColumnType("text[]");
        builder.Property(m => m.IpAddress).HasColumnName("ip_address");
    }
}
