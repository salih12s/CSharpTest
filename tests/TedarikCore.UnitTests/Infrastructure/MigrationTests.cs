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
    public void InitialCatalog_Migration_Exists_And_IsTheOnlyMigration()
    {
        using var context = CreateContext();

        var migration = Assert.Single(context.Database.GetMigrations());
        Assert.EndsWith("_InitialCatalog", migration);
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
