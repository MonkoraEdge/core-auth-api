using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MonkoraEdge.Core.Auth.Infrastructure.DbContexts.EntityTypeConfigurations;

public class UserExternalLoginEntityTypeConfiguration : IEntityTypeConfiguration<UserExternalLogin>
{
    public void Configure(EntityTypeBuilder<UserExternalLogin> builder)
    {
        builder.ToTable("lnk_user_external_logins");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Scopes).HasColumnType("text[]");

        // Composite index for GetByProviderUserIdAsync (called on every social/external login callback).
        builder.HasIndex(m => new { m.ProviderId, m.ProviderUserId });
        // Index for GetByUserIdAsync (list all external logins for a user).
        builder.HasIndex(m => m.UserId);
    }
}
