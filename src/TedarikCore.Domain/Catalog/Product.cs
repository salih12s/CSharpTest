namespace TedarikCore.Domain.Catalog;

public class Product
{
    public const int MaxSkuLength = 64;
    public const int MaxNameLength = 200;

    public Guid Id { get; private set; }
    public string Sku { get; private set; }
    public string Name { get; private set; }
    public long PriceKurus { get; private set; }
    public bool IsActive { get; private set; }

    // EF Core nesneyi veritabanından okurken kullanır; kod tarafından çağrılmaz.
    private Product()
    {
        Sku = null!;
        Name = null!;
    }

    private Product(string sku, string name, long priceKurus)
    {
        Id = Guid.NewGuid();
        Sku = sku;
        Name = name;
        PriceKurus = priceKurus;
        IsActive = true;
    }

    public static Product Create(string sku, string name, long priceKurus)
    {
        return new Product(
            NormalizeSku(sku),
            NormalizeName(name),
            ValidatePrice(priceKurus));
    }

    public void ChangePrice(long newPriceKurus)
    {
        PriceKurus = ValidatePrice(newPriceKurus);
    }

    private static string NormalizeSku(string? sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ArgumentException("SKU boş olamaz.", nameof(sku));
        }

        var normalized = sku.Trim().ToUpperInvariant();
        if (normalized.Length > MaxSkuLength)
        {
            throw new ArgumentException($"SKU en fazla {MaxSkuLength} karakter olabilir.", nameof(sku));
        }

        return normalized;
    }

    private static string NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Ürün adı boş olamaz.", nameof(name));
        }

        var normalized = name.Trim();
        if (normalized.Length > MaxNameLength)
        {
            throw new ArgumentException($"Ürün adı en fazla {MaxNameLength} karakter olabilir.", nameof(name));
        }

        return normalized;
    }

    private static long ValidatePrice(long priceKurus)
    {
        if (priceKurus <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(priceKurus), priceKurus, "Fiyat sıfırdan büyük olmalı (kuruş cinsinden).");
        }

        return priceKurus;
    }
}
