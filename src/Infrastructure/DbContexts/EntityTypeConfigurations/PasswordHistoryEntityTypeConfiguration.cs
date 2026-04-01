using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MonkoraEdge.Core.Auth.Infrastructure.DbContexts.EntityTypeConfigurations;

public class PasswordHistoryEntityTypeConfiguration : IEntityTypeConfiguration<PasswordHistory>
{
    public void Configure(EntityTypeBuilder<PasswordHistory> builder)
    {
        builder.ToTable("tx_password_history");
        builder.HasKey(m => m.Id);

        builder.HasIndex(m => new { m.UserId, m.CreatedAt }); // GetByUserIdAsync with ORDER BY + LIMIT
    }
}
