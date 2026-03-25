using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MonkoraEdge.Core.Auth.Infrastructure.DbContexts.EntityTypeConfigurations;

public class AuthorizationClientEntityTypeConfiguration : IEntityTypeConfiguration<AuthorizationClient>
{
    public void Configure(EntityTypeBuilder<AuthorizationClient> builder)
    {
        builder.ToTable("mt_authorization_clients");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.RedirectUris).HasColumnType("text[]");
        builder.Property(m => m.PostLogoutRedirectUris).HasColumnType("text[]");
        builder.Property(m => m.AllowedGrantTypes).HasColumnType("text[]");
        builder.Property(m => m.AllowedResponseTypes).HasColumnType("text[]");

        builder.Property(m => m.ClientType).HasDefaultValue("CONFIDENTIAL");
        builder.Property(m => m.TokenEndpointAuthMethod).HasDefaultValue("CLIENT_SECRET_BASIC");
        builder.Property(m => m.AccessTokenLifetime).HasDefaultValue(3600);
        builder.Property(m => m.RefreshTokenLifetime).HasDefaultValue(2592000);

        builder.HasIndex(m => m.ClientId).IsUnique();
    }
}
