using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MonkoraEdge.Core.Auth.Infrastructure.DbContexts.EntityTypeConfigurations;

public class AuthorizationCodeScopeEntityTypeConfiguration : IEntityTypeConfiguration<AuthorizationCodeScope>
{
    public void Configure(EntityTypeBuilder<AuthorizationCodeScope> builder)
    {
        builder.ToTable("lnk_authorization_code_scopes");
        builder.HasKey(m => m.Id);
    }
}
