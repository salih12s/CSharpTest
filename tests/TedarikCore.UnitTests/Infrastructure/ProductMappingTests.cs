using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using TedarikCore.Domain.Catalog;
using TedarikCore.Infrastructure.Persistence;

namespace TedarikCore.UnitTests.Infrastructure;

// Model veritabanına bağlanmadan kurulur; bu testler PostgreSQL gerektirmez.
public class ProductMappingTests
{
    private static AppDbContext CreateContext()
    {
        // Bağlantı hiç açılmaz; port 1 kimsenin dinlemediği bir adres, yanlışlıkla bağlanılırsa test kırılır.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Port=1;Database=yok;Username=yok;Password=yok;Timeout=1")
            .Options;
        return new AppDbContext(options);
    }

    private static Microsoft.EntityFrameworkCore.Metadata.IEntityType ProductEntity(AppDbContext context) =>
        context.Model.FindEntityType(typeof(Product))!;

    [Fact]
    public void Product_IsMappedToProductsTable()
    {
        using var context = CreateContext();

        Assert.Equal("products", ProductEntity(context).GetTableName());
    }

    [Theory]
    [InlineData(nameof(Product.Id), "id")]
    [InlineData(nameof(Product.Sku), "sku")]
    [InlineData(nameof(Product.Name), "name")]
    [InlineData(nameof(Product.PriceKurus), "price_kurus")]
    [InlineData(nameof(Product.IsActive), "is_active")]
    public void Product_Properties_AreMappedToSnakeCaseColumns(string property, string column)
    {
        using var context = CreateContext();
        var entity = ProductEntity(context);
        var table = Microsoft.EntityFrameworkCore.Metadata.StoreObjectIdentifier.Table("products");

        Assert.Equal(column, entity.FindProperty(property)!.GetColumnName(table));
    }

    [Fact]
    public void Product_Id_IsPrimaryKey_Uuid_NotGeneratedByDatabase()
    {
        using var context = CreateContext();
        var entity = ProductEntity(context);
        var table = Microsoft.EntityFrameworkCore.Metadata.StoreObjectIdentifier.Table("products");

        Assert.Equal(nameof(Product.Id), Assert.Single(entity.FindPrimaryKey()!.Properties).Name);
        Assert.Equal("uuid", entity.FindProperty(nameof(Product.Id))!.GetColumnType());
        Assert.Equal(
            Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never,
            entity.FindProperty(nameof(Product.Id))!.ValueGenerated);
        Assert.Equal("id", entity.FindProperty(nameof(Product.Id))!.GetColumnName(table));
    }

    [Fact]
    public void Product_Sku_HasUniqueIndex_And_MaxLength64()
    {
        using var context = CreateContext();
        var entity = ProductEntity(context);

        var index = Assert.Single(
            entity.GetIndexes(),
            i => i.Properties.Select(p => p.Name).SequenceEqual([nameof(Product.Sku)]));
        Assert.True(index.IsUnique);
        Assert.Equal(64, entity.FindProperty(nameof(Product.Sku))!.GetMaxLength());
        Assert.False(entity.FindProperty(nameof(Product.Sku))!.IsNullable);
    }

    [Fact]
    public void Product_Name_IsRequired_And_MaxLength200()
    {
        using var context = CreateContext();
        var property = ProductEntity(context).FindProperty(nameof(Product.Name))!;

        Assert.False(property.IsNullable);
        Assert.Equal(200, property.GetMaxLength());
    }

    [Fact]
    public void Products_Table_HasCheck_PriceKurus_GreaterThanZero_And_Sku_Uppercase()
    {
        using var context = CreateContext();
        var table = context.GetService<IDesignTimeModel>().Model
            .GetRelationalModel()
            .FindTable("products", null)!;

        var checks = table.CheckConstraints.ToDictionary(c => c.Name!, c => c.Sql);

        Assert.Equal(2, checks.Count);
        Assert.Equal("price_kurus > 0", checks["ck_products_price_kurus_positive"]);
        Assert.Equal("sku = upper(sku)", checks["ck_products_sku_uppercase"]);
    }

    [Fact]
    public void Product_PriceKurus_IsBigint_And_IsActive_IsBoolean()
    {
        using var context = CreateContext();
        var entity = ProductEntity(context);

        Assert.Equal("bigint", entity.FindProperty(nameof(Product.PriceKurus))!.GetColumnType());
        Assert.Equal("boolean", entity.FindProperty(nameof(Product.IsActive))!.GetColumnType());
    }
}
