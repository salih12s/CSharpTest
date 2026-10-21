using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TedarikCore.Infrastructure.Persistence;
using TedarikCore.Infrastructure.Persistence.Migrations;

namespace TedarikCore.UnitTests.Infrastructure;

// Migration'lar burada yalnızca okunur ve karşılaştırılır; veritabanına hiçbir şey uygulanmaz.
public class MigrationTests
{
    private static AppDbContext CreateContext()
    {
        // Port 1: yanlışlıkla bağlanılırsa test kırılır, gerçek veritabanına ulaşılamaz.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Port=1;Database=yok;Username=yok;Password=yok;Timeout=1")
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public void Migrations_AreInitialCatalog_Then_EnforceUppercaseSku_InOrder()
    {
        using var context = CreateContext();

        var migrations = context.Database.GetMigrations().ToList();

        Assert.Equal(2, migrations.Count);
        Assert.EndsWith("_InitialCatalog", migrations[0]);
        Assert.EndsWith("_EnforceUppercaseSku", migrations[1]);
    }

    [Fact]
    public void EnforceUppercaseSku_Up_OnlyAddsUppercaseCheck_OnProducts()
    {
        var operations = new EnforceUppercaseSku().UpOperations;

        var add = Assert.IsType<AddCheckConstraintOperation>(Assert.Single(operations));
        Assert.Equal("ck_products_sku_uppercase", add.Name);
        Assert.Equal("products", add.Table);
        Assert.Equal("sku = upper(sku)", add.Sql);
    }

    [Fact]
    public void EnforceUppercaseSku_Down_OnlyDropsItsOwnCheck_NotTheTable()
    {
        var operations = new EnforceUppercaseSku().DownOperations;

        var drop = Assert.IsType<DropCheckConstraintOperation>(Assert.Single(operations));
        Assert.Equal("ck_products_sku_uppercase", drop.Name);
        Assert.Equal("products", drop.Table);
    }

    [Fact]
    public void InitialCatalog_WasNotRewritten_ItStillHasOnlyThePriceCheck()
    {
        // InitialCatalog test veritabanına uygulandı; değiştirilmemeli. Yeni kural yalnızca yeni migration'dadır.
        var table = Assert.Single(new InitialCatalog().UpOperations.OfType<CreateTableOperation>());

        var check = Assert.Single(table.CheckConstraints);
        Assert.Equal("ck_products_price_kurus_positive", check.Name);
    }

    [Fact]
    public void Model_Matches_LatestMigrationSnapshot()
    {
        using var context = CreateContext();

        // Entity ya da eşleme değişip yeni migration üretilmediyse bu test kırılır.
        Assert.False(
            context.Database.HasPendingModelChanges(),
            "Model, son migration anlık görüntüsüyle uyuşmuyor. Yeni migration üretilmeli.");
    }

    [Fact]
    public void InitialCatalog_Up_CreatesOnlyProductsTable_WithUniqueSkuIndex_And_PriceCheck()
    {
        var operations = new InitialCatalog().UpOperations;

        var table = Assert.Single(operations.OfType<CreateTableOperation>());
        Assert.Equal("products", table.Name);

        var check = Assert.Single(table.CheckConstraints);
        Assert.Equal("ck_products_price_kurus_positive", check.Name);
        Assert.Equal("price_kurus > 0", check.Sql);

        var index = Assert.Single(operations.OfType<CreateIndexOperation>());
        Assert.Equal("ix_products_sku", index.Name);
        Assert.Equal("products", index.Table);
        Assert.True(index.IsUnique);
        Assert.Equal(["sku"], index.Columns);
    }

    [Fact]
    public void InitialCatalog_Up_ContainsNoDestructiveOperations_And_TouchesOnlyProducts()
    {
        var operations = new InitialCatalog().UpOperations;

        Assert.All(operations, op => Assert.True(
            op is CreateTableOperation or CreateIndexOperation,
            $"Beklenmeyen işlem: {op.GetType().Name}"));
        Assert.DoesNotContain(operations, op => op is SqlOperation or DropTableOperation or DeleteDataOperation);
    }

    [Fact]
    public void InitialCatalog_Down_OnlyDropsProductsTable()
    {
        var operations = new InitialCatalog().DownOperations;

        var drop = Assert.Single(operations);
        Assert.Equal("products", Assert.IsType<DropTableOperation>(drop).Name);
    }
}
