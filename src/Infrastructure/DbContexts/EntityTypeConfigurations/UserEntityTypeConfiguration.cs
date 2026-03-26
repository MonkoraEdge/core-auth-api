using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MonkoraEdge.Core.Auth.Infrastructure.DbContexts.EntityTypeConfigurations;

public class UserEntityTypeConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("mt_users");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.LocaleCode).HasColumnName("locale");
        builder.Property(m => m.Status).HasDefaultValue("INACTIVE");
        builder.Property(m => m.RegistrationSource).HasDefaultValue("LOCAL");

        builder.HasIndex(m => m.Email).IsUnique();
    }
}
