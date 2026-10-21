using System.Globalization;
using TedarikCore.Domain.Catalog;

namespace TedarikCore.UnitTests.Domain.Catalog;

public class ProductTests
{
    private static Product CreateValid() => Product.Create("ABC-1", "Kalem", 1250);

    [Fact]
    public void Create_WithValidValues_SetsProperties_And_IsActive()
    {
        var product = Product.Create("ABC-1", "Kalem", 1250);

        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal("ABC-1", product.Sku);
        Assert.Equal("Kalem", product.Name);
        Assert.Equal(1250, product.PriceKurus);
        Assert.True(product.IsActive);
    }

    [Fact]
    public void Create_Twice_GeneratesDifferentIds()
    {
        Assert.NotEqual(CreateValid().Id, CreateValid().Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptySku_Throws(string? sku)
    {
        Assert.Throws<ArgumentException>(() => Product.Create(sku!, "Kalem", 1250));
    }

    [Theory]
    [InlineData("  abc-1  ", "ABC-1")]
    [InlineData("abc-1", "ABC-1")]
    [InlineData("Abc-1", "ABC-1")]
    [InlineData("\tabc 1\n", "ABC 1")]
    public void Create_NormalizesSku_TrimAndUpperCase(string input, string expected)
    {
        var product = Product.Create(input, "Kalem", 1250);

        Assert.Equal(expected, product.Sku);
    }

    [Fact]
    public void Create_NormalizesSku_WithInvariantCulture_NotTurkish()
    {
        // Türkçe kültürde ToUpper() "i" -> "İ" yapar; SKU her makinede aynı çıkmalı.
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
        try
        {
            var product = Product.Create("kalem-i", "Kalem", 1250);

            Assert.Equal("KALEM-I", product.Sku);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Create_WithSku_AtMaxLength_Succeeds()
    {
        var product = Product.Create(new string('a', Product.MaxSkuLength), "Kalem", 1250);

        Assert.Equal(Product.MaxSkuLength, product.Sku.Length);
    }

    [Fact]
    public void Create_WithSku_OverMaxLength_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => Product.Create(new string('a', Product.MaxSkuLength + 1), "Kalem", 1250));
    }

    [Fact]
    public void Create_WithSku_OverMaxLength_OnlyBecauseOfSpaces_Succeeds()
    {
        // Uzunluk kırpıldıktan sonra ölçülür; baştaki/sondaki boşluk sınırı aşırmaz.
        var sku = "  " + new string('a', Product.MaxSkuLength) + "  ";

        var product = Product.Create(sku, "Kalem", 1250);

        Assert.Equal(Product.MaxSkuLength, product.Sku.Length);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_Throws(string? name)
    {
        Assert.Throws<ArgumentException>(() => Product.Create("ABC-1", name!, 1250));
    }

    [Fact]
    public void Create_TrimsName_ButKeepsCase()
    {
        var product = Product.Create("ABC-1", "  Tükenmez Kalem  ", 1250);

        Assert.Equal("Tükenmez Kalem", product.Name);
    }

    [Fact]
    public void Create_WithName_AtMaxLength_Succeeds()
    {
        var product = Product.Create("ABC-1", new string('a', Product.MaxNameLength), 1250);

        Assert.Equal(Product.MaxNameLength, product.Name.Length);
    }

    [Fact]
    public void Create_WithName_OverMaxLength_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => Product.Create("ABC-1", new string('a', Product.MaxNameLength + 1), 1250));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(long.MinValue)]
    public void Create_WithZeroOrNegativePrice_Throws(long price)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Product.Create("ABC-1", "Kalem", price));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(long.MaxValue)]
    public void Create_WithSmallestAndLargestPrice_Succeeds(long price)
    {
        Assert.Equal(price, Product.Create("ABC-1", "Kalem", price).PriceKurus);
    }

    [Fact]
    public void ChangePrice_WithValidPrice_UpdatesPrice()
    {
        var product = CreateValid();

        product.ChangePrice(9900);

        Assert.Equal(9900, product.PriceKurus);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ChangePrice_WithInvalidPrice_Throws_And_KeepsOldPrice(long invalidPrice)
    {
        var product = CreateValid();

        Assert.Throws<ArgumentOutOfRangeException>(() => product.ChangePrice(invalidPrice));

        Assert.Equal(1250, product.PriceKurus);
    }

    [Fact]
    public void Properties_HaveNoPublicSetters()
    {
        var publicSetters = typeof(Product)
            .GetProperties()
            .Where(p => p.SetMethod is { IsPublic: true })
            .Select(p => p.Name)
            .ToList();

        Assert.Empty(publicSetters);
    }
}
