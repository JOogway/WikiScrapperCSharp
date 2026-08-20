using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WikiScrapper.Domain.Entities;

namespace WikiScrapper.Data.Configurations;

/// <summary>EF Core mapping for <see cref="Voivodeship"/>.</summary>
public class VoivodeshipConfiguration : IEntityTypeConfiguration<Voivodeship>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Voivodeship> builder)
    {
        builder.Property(v => v.Name).HasMaxLength(100).IsRequired();
        builder.Property(v => v.WikiTitle).HasMaxLength(200).IsRequired();
        builder.Property(v => v.WikiUrl).HasMaxLength(500);

        builder.HasIndex(v => v.Name).IsUnique();
    }
}
