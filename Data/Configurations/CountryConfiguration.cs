using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WikiScrapper.Domain.Entities;

namespace WikiScrapper.Data.Configurations;

/// <summary>EF Core mapping for <see cref="Country"/>.</summary>
public class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.Property(c => c.Name).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Code).HasMaxLength(2).IsFixedLength().IsRequired();
        builder.Property(c => c.WikiTitle).HasMaxLength(200).IsRequired();
        builder.Property(c => c.WikiUrl).HasMaxLength(500);

        builder.HasIndex(c => c.Code).IsUnique();
        builder.HasIndex(c => c.Name).IsUnique();
    }
}
