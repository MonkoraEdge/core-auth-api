using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

namespace MonkoraEdge.Core.Auth.Infrastructure.DbContexts.EntityTypeConfigurations;

public class SamlProviderEntityTypeConfiguration : IEntityTypeConfiguration<SamlProvider>
{
    public void Configure(EntityTypeBuilder<SamlProvider> builder)
    {
        builder.ToTable("mt_saml_providers");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ProviderCode).HasMaxLength(64).IsRequired();
        builder.Property(e => e.DisplayName).HasMaxLength(256).IsRequired();

        // SP
        builder.Property(e => e.SpEntityId).HasMaxLength(512).IsRequired();
        builder.Property(e => e.SpCertificatePem).HasColumnType("text");
        builder.Property(e => e.SpPrivateKeyEncrypted).HasColumnType("text");

        // IdP
        builder.Property(e => e.IdpEntityId).HasMaxLength(512).IsRequired();
        builder.Property(e => e.IdpSsoUrl).HasMaxLength(2048).IsRequired();
        builder.Property(e => e.IdpSloUrl).HasMaxLength(2048);
        builder.Property(e => e.IdpCertificatePem).HasColumnType("text").IsRequired();
        builder.Property(e => e.IdpMetadataUrl).HasMaxLength(2048);

        // Behavior
        builder.Property(e => e.NameIdFormat).HasMaxLength(256);
        builder.Property(e => e.AttributeMappingJson).HasColumnType("jsonb");

        // ProviderCode must be globally unique for URL routing.
        builder.HasIndex(e => e.ProviderCode).IsUnique();
    }
}
