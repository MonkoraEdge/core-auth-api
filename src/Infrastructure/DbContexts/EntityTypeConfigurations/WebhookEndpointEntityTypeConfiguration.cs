using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

namespace MonkoraEdge.Core.Auth.Infrastructure.DbContexts.EntityTypeConfigurations;

public class WebhookEndpointEntityTypeConfiguration : IEntityTypeConfiguration<WebhookEndpoint>
{
    public void Configure(EntityTypeBuilder<WebhookEndpoint> builder)
    {
        builder.ToTable("mt_webhook_endpoints");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Url).HasMaxLength(2048).IsRequired();
        builder.Property(e => e.Events).HasMaxLength(1024).IsRequired();
        builder.Property(e => e.Secret).HasMaxLength(256).IsRequired();

        builder.HasIndex(e => e.ClientId);
        builder.HasIndex(e => new { e.IsActive, e.IsDeleted });
    }
}
