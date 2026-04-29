using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

namespace MonkoraEdge.Core.Auth.Infrastructure.DbContexts.EntityTypeConfigurations;

public class WebhookDeliveryLogEntityTypeConfiguration : IEntityTypeConfiguration<WebhookDeliveryLog>
{
    public void Configure(EntityTypeBuilder<WebhookDeliveryLog> builder)
    {
        builder.ToTable("tx_webhook_delivery_logs");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.EventType).HasMaxLength(128).IsRequired();
        builder.Property(l => l.Payload).HasColumnType("jsonb");
        builder.Property(l => l.ErrorMessage).HasMaxLength(1024);

        builder.HasIndex(l => new { l.WebhookEndpointId, l.CreatedAt });
        builder.HasIndex(l => new { l.Success, l.CreatedAt });
    }
}
