using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

namespace MonkoraEdge.Core.Auth.Infrastructure.DbContexts.EntityTypeConfigurations;

public class PasskeyCredentialEntityTypeConfiguration : IEntityTypeConfiguration<PasskeyCredential>
{
    public void Configure(EntityTypeBuilder<PasskeyCredential> builder)
    {
        builder.ToTable("mt_passkey_credentials");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.CredentialId).IsRequired();
        builder.Property(e => e.CredentialIdBase64Url).HasMaxLength(512).IsRequired();
        builder.Property(e => e.PublicKey).IsRequired();
        builder.Property(e => e.AaGuid).HasMaxLength(64).IsRequired();
        builder.Property(e => e.FriendlyName).HasMaxLength(128);
        builder.Property(e => e.AttestationType).HasMaxLength(64);

        // Store transport hints as jsonb
        builder.Property(e => e.Transports).HasColumnType("jsonb");

        // credential_id_base64_url must be globally unique.
        builder.HasIndex(e => e.CredentialIdBase64Url).IsUnique();

        // Efficient lookup of all passkeys belonging to a user.
        builder.HasIndex(e => e.UserId);
    }
}
