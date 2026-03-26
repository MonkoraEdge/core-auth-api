using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MonkoraEdge.Core.Auth.Infrastructure.DbContexts.EntityTypeConfigurations;

public class RateLimitEntityTypeConfiguration : IEntityTypeConfiguration<RateLimit>
{
    public void Configure(EntityTypeBuilder<RateLimit> builder)
    {
        builder.ToTable("tx_rate_limits");
        builder.HasKey(m => m.Id);
        builder.Ignore(m => m.Description);
    }
}
