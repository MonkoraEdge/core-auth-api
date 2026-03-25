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
    }
}
