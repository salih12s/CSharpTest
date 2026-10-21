using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TedarikCore.Domain.Catalog;

namespace TedarikCore.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id).HasName("pk_products");
        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(p => p.Sku)
            .HasColumnName("sku")
            .HasMaxLength(Product.MaxSkuLength)
            .IsRequired();
        builder.HasIndex(p => p.Sku)
            .IsUnique()
            .HasDatabaseName("ix_products_sku");

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(Product.MaxNameLength)
            .IsRequired();

        builder.Property(p => p.PriceKurus)
            .HasColumnName("price_kurus")
            .HasColumnType("bigint");

        builder.Property(p => p.IsActive)
            .HasColumnName("is_active")
            .HasColumnType("boolean");
    }
}
