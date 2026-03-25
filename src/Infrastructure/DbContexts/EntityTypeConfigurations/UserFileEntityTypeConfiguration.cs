using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MonkoraEdge.Core.Auth.Infrastructure.DbContexts.EntityTypeConfigurations;

public class UserFileEntityTypeConfiguration : IEntityTypeConfiguration<UserFile>
{
    public void Configure(EntityTypeBuilder<UserFile> builder)
    {
        builder.ToTable("tx_user_files");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.FileName).HasColumnType("jsonb");
        builder.Property(m => m.FileType).HasDefaultValue("OTHER");
    }
}
