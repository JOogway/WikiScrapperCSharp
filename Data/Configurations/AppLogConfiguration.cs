using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WikiScrapper.Domain;
using WikiScrapper.Domain.Entities;

namespace WikiScrapper.Data.Configurations;

/// <summary>EF Core mapping for <see cref="AppLog"/>.</summary>
public class AppLogConfiguration : IEntityTypeConfiguration<AppLog>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AppLog> builder)
    {
        builder.Property(l => l.Level)
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(new EnumToStringConverter<AppLogLevel>());
        builder.Property(l => l.Message).HasMaxLength(2000).IsRequired();
        builder.Property(l => l.Source).HasMaxLength(100).IsRequired();

        builder.HasIndex(l => l.CreatedAt);
    }
}
