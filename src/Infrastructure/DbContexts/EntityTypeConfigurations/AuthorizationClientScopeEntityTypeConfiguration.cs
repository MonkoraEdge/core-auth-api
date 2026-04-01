using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MonkoraEdge.Core.Auth.Infrastructure.DbContexts.EntityTypeConfigurations;

public class AuthorizationClientScopeEntityTypeConfiguration : IEntityTypeConfiguration<AuthorizationClientScope>
{
    public void Configure(EntityTypeBuilder<AuthorizationClientScope> builder)
    {
        builder.ToTable("lnk_authorization_client_scopes");
        builder.HasKey(m => m.Id);

        // Enforce uniqueness — a scope can only be assigned to a client once.
        // Matches the SQL constraint: uq_lnk_authorization_client_scopes_client_scope
        builder.HasIndex(m => new { m.ClientId, m.ScopeId }).IsUnique();

        // Support efficient GetByClientIdAsync lookups.
        builder.HasIndex(m => m.ClientId);
    }
}
