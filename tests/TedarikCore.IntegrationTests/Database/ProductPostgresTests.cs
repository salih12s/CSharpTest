using Microsoft.EntityFrameworkCore;
using Npgsql;
using TedarikCore.Domain.Catalog;
using TedarikCore.Infrastructure.Persistence;

namespace TedarikCore.IntegrationTests.Database;

// Gerçek PostgreSQL (tedarikcore_test) üzerinde çalışır. Önkoşul: InitialCatalog migration'ı bu
// veritabanına uygulanmış olmalı (bu testler migration uygulamaz).
//
// Veri izolasyonu: her test benzersiz bir SKU kullanır ve yazdıklarını açık bir transaction içinde
// yapıp hiç commit etmeden geri alır (rollback). Tek istisna eşzamanlılık testidir (iki ayrı bağlantı
// commit gerektirir); o test yazdığı satırları benzersiz SKU ile bulup siler.
[Collection(PostgresTestCollection.Name)]
public class ProductPostgresTests
{
    private static string UniqueSku(string label) => $"T06-{label}-{Guid.NewGuid():N}".ToUpperInvariant();

    private static AppDbContext CreateContext(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        var context = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options);
        context.Database.UseTransaction(transaction);
        return context;
    }

    private static async Task<int> InsertRawAsync(
        NpgsqlConnection connection, NpgsqlTransaction? transaction, string sku, long priceKurus)
    {
        await using var command = new NpgsqlCommand(
            "insert into products (id, sku, name, price_kurus, is_active) values (@id, @sku, 'Ham SQL', @price, true)",
            connection, transaction);
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("sku", sku);
        command.Parameters.AddWithValue("price", priceKurus);
        return await command.ExecuteNonQueryAsync();
    }

    private static async Task<long> CountBySkuPrefixAsync(
        NpgsqlConnection connection, NpgsqlTransaction? transaction, string skuPrefix)
    {
        await using var command = new NpgsqlCommand(
            "select count(*) from products where sku like @prefix", connection, transaction);
        command.Parameters.AddWithValue("prefix", skuPrefix + "%");
        return (long)(await command.ExecuteScalarAsync())!;
    }

    [RequiresTestDatabaseFact]
    public async Task ValidProduct_CanBeSaved_And_ReadBack()
    {
        await using var connection = await PostgresTestDatabase.OpenVerifiedConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var context = CreateContext(connection, transaction);
        var product = Product.Create(UniqueSku("okuma"), "  Tükenmez Kalem  ", 1250);

        context.Products.Add(product);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear(); // izlenen nesneyi at: aşağıdaki okuma gerçekten veritabanından gelir

        var loaded = await context.Products.SingleAsync(p => p.Id == product.Id);
        Assert.Equal(product.Sku, loaded.Sku);
        Assert.Equal("Tükenmez Kalem", loaded.Name);
        Assert.Equal(1250, loaded.PriceKurus);
        Assert.True(loaded.IsActive);
    }

    [RequiresTestDatabaseFact]
    public async Task ChangedPrice_IsPersisted_ThroughPrivateSetter()
    {
        await using var connection = await PostgresTestDatabase.OpenVerifiedConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var context = CreateContext(connection, transaction);
        var product = Product.Create(UniqueSku("fiyat"), "Kalem", 1250);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        product.ChangePrice(9900);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var loaded = await context.Products.SingleAsync(p => p.Id == product.Id);
        Assert.Equal(9900, loaded.PriceKurus);
    }

    [RequiresTestDatabaseFact]
    public async Task Products_WithDifferentSkus_CanBeSaved()
    {
        await using var connection = await PostgresTestDatabase.OpenVerifiedConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var context = CreateContext(connection, transaction);
        var prefix = $"T06-FARKLI-{Guid.NewGuid():N}";

        context.Products.Add(Product.Create(prefix + "-A", "A", 100));
        context.Products.Add(Product.Create(prefix + "-B", "B", 200));
        var saved = await context.SaveChangesAsync();

        Assert.Equal(2, saved);
        Assert.Equal(2, await CountBySkuPrefixAsync(connection, transaction, prefix.ToUpperInvariant()));
    }

    [RequiresTestDatabaseFact]
    public async Task SameSku_SavedTwice_FailsWith_23505_UniqueViolation()
    {
        await using var connection = await PostgresTestDatabase.OpenVerifiedConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var context = CreateContext(connection, transaction);
        var sku = UniqueSku("cift");
        context.Products.Add(Product.Create(sku, "Birinci", 100));
        await context.SaveChangesAsync();

        context.Products.Add(Product.Create(sku, "İkinci", 200));
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        var pg = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal("23505", pg.SqlState);
        Assert.Equal("ix_products_sku", pg.ConstraintName);
    }

    [RequiresTestDatabaseFact]
    public async Task RawSql_WithPositivePrice_Succeeds_Control()
    {
        // Kontrol: ham INSERT cümlesi kendi başına geçerli; aşağıdaki hatalar yalnızca fiyattan gelir.
        await using var connection = await PostgresTestDatabase.OpenVerifiedConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        var inserted = await InsertRawAsync(connection, transaction, UniqueSku("kontrol"), 1);

        Assert.Equal(1, inserted);
    }

    [RequiresTestDatabaseFact]
    public async Task RawSql_ZeroPrice_FailsWith_23514_CheckViolation()
    {
        await using var connection = await PostgresTestDatabase.OpenVerifiedConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        var ex = await Assert.ThrowsAsync<PostgresException>(
            () => InsertRawAsync(connection, transaction, UniqueSku("sifir"), 0));

        Assert.Equal("23514", ex.SqlState);
        Assert.Equal("ck_products_price_kurus_positive", ex.ConstraintName);
    }

    [RequiresTestDatabaseFact]
    public async Task RawSql_NegativePrice_FailsWith_23514_CheckViolation()
    {
        await using var connection = await PostgresTestDatabase.OpenVerifiedConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        var ex = await Assert.ThrowsAsync<PostgresException>(
            () => InsertRawAsync(connection, transaction, UniqueSku("eksi"), -1));

        Assert.Equal("23514", ex.SqlState);
        Assert.Equal("ck_products_price_kurus_positive", ex.ConstraintName);
    }

    [RequiresTestDatabaseFact]
    public async Task RawSql_Update_ToZeroPrice_FailsWith_23514_CheckViolation()
    {
        await using var connection = await PostgresTestDatabase.OpenVerifiedConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        var sku = UniqueSku("guncelle");
        await InsertRawAsync(connection, transaction, sku, 500);

        await using var update = new NpgsqlCommand(
            "update products set price_kurus = 0 where sku = @sku", connection, transaction);
        update.Parameters.AddWithValue("sku", sku);
        var ex = await Assert.ThrowsAsync<PostgresException>(() => update.ExecuteNonQueryAsync());

        Assert.Equal("23514", ex.SqlState);
    }

    // ADIM 06'da bu test "indeks harf büyüklüğüne duyarlı, küçük harfli kopya EKLENEBİLİYOR" davranışını
    // belgeliyordu. ck_products_sku_uppercase (EnforceUppercaseSku migration'ı) ile artık reddedilir.
    [RequiresTestDatabaseFact]
    public async Task Sku_LowercaseVariantOfExistingSku_IsRejected_ByUppercaseCheck()
    {
        await using var connection = await PostgresTestDatabase.OpenVerifiedConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        var suffix = Guid.NewGuid().ToString("N").ToUpperInvariant();

        var first = await InsertRawAsync(connection, transaction, $"T07-HARF-{suffix}", 100);
        var ex = await Assert.ThrowsAsync<PostgresException>(
            () => InsertRawAsync(connection, transaction, $"t07-harf-{suffix}", 100));

        Assert.Equal(1, first);
        Assert.Equal("23514", ex.SqlState);
        Assert.Equal("ck_products_sku_uppercase", ex.ConstraintName);
    }

    // Kontrol çifti: aynı karakterlerin BÜYÜK harfli hâli kabul edilir (red nedeni yalnızca küçük harf).
    [RequiresTestDatabaseTheory]
    [InlineData("T07-ABC")]
    [InlineData("T07-ÖLÇÜŞĞ")]
    public async Task RawSql_UppercaseSku_Succeeds_Control(string skuBase)
    {
        await using var connection = await PostgresTestDatabase.OpenVerifiedConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        var inserted = await InsertRawAsync(
            connection, transaction, $"{skuBase}-{Guid.NewGuid():N}".ToUpperInvariant(), 100);

        Assert.Equal(1, inserted);
    }

    [RequiresTestDatabaseTheory]
    [InlineData("t07-abc")] // tamamen küçük ASCII
    [InlineData("T07-Abc")] // karışık harf
    [InlineData("T07-ÖLÇÜşğ")] // Türkçe küçük harfler (ş, ğ)
    [InlineData("T07-ölçü")] // tamamen küçük Türkçe
    public async Task RawSql_SkuWithLowercaseLetters_FailsWith_23514_UppercaseCheck(string skuBase)
    {
        await using var connection = await PostgresTestDatabase.OpenVerifiedConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        // Sonekin harfleri BÜYÜK: red yalnızca skuBase içindeki küçük harften gelir.
        var sku = skuBase + "-" + Guid.NewGuid().ToString("N").ToUpperInvariant();

        var ex = await Assert.ThrowsAsync<PostgresException>(() => InsertRawAsync(connection, transaction, sku, 100));

        Assert.Equal("23514", ex.SqlState);
        Assert.Equal("ck_products_sku_uppercase", ex.ConstraintName);
    }

    // Product.Create'in ürettiği SKU'lar (özellikle Türkçe karakterli olanlar) CHECK tarafından
    // reddedilmemeli: C# ToUpperInvariant ile PostgreSQL upper() uyumunun gerçek veritabanı kanıtı.
    [RequiresTestDatabaseTheory]
    [InlineData("abc-1")]
    [InlineData("kalem-i")]
    [InlineData("ışık-öğe-çüş")]
    [InlineData("İSTANBUL-ı")]
    [InlineData("straße-01")]
    [InlineData("çiğdem")]
    [InlineData("ǆ-ǅ")]
    [InlineData("ﬁ-ligatur")]
    public async Task ProductCreate_NormalizedSkus_AreAccepted_By_UppercaseCheck(string skuBase)
    {
        await using var connection = await PostgresTestDatabase.OpenVerifiedConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var context = CreateContext(connection, transaction);
        var product = Product.Create($"{skuBase}-{Guid.NewGuid():N}", "Test", 100);

        context.Products.Add(product);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var loaded = await context.Products.SingleAsync(p => p.Id == product.Id);
        Assert.Equal(product.Sku, loaded.Sku);
        Assert.Equal(product.Sku, product.Sku.ToUpperInvariant());
    }

    [RequiresTestDatabaseFact]
    public async Task ConcurrentInserts_OfSameSku_ExactlyOneWins_Other_Gets23505()
    {
        var sku = UniqueSku("yaris");

        // Test gövdesi ne olursa olsun temizlik taze bir bağlantıyla ve yalnızca bu TAM SKU için çalışır;
        // temizlik hatası da asıl test hatası da kaybolmaz (bkz. RunWithCleanupAsync).
        await PostgresTestDatabase.RunWithCleanupAsync(
            async () =>
            {
                await using var first = await PostgresTestDatabase.OpenVerifiedConnectionAsync();
                await using var second = await PostgresTestDatabase.OpenVerifiedConnectionAsync();

                // İki ayrı bağlantı, otomatik commit: gerçekten eşzamanlı iki istek gibi.
                using var gate = new ManualResetEventSlim(false);
                Task<Exception?> Insert(NpgsqlConnection connection) => Task.Run(async () =>
                {
                    gate.Wait();
                    try
                    {
                        await InsertRawAsync(connection, null, sku, 100);
                        return (Exception?)null;
                    }
                    catch (Exception e)
                    {
                        return e;
                    }
                });
                var tasks = new[] { Insert(first), Insert(second) };
                gate.Set();
                var results = await Task.WhenAll(tasks);

                var failures = results.Where(r => r is not null).ToList();
                Assert.Single(failures);
                Assert.Equal("23505", Assert.IsType<PostgresException>(failures[0]).SqlState);
                Assert.Equal(1, await CountBySkuPrefixAsync(first, null, sku));
            },
            () => PostgresTestDatabase.DeleteProductBySkuAsync(sku));
    }

    [RequiresTestDatabaseFact]
    public async Task RolledBackTransaction_LeavesNoRows()
    {
        // İzolasyon yönteminin kendisini kanıtlar: transaction geri alınınca satır kalmaz.
        var sku = UniqueSku("rollback");
        await using (var connection = await PostgresTestDatabase.OpenVerifiedConnectionAsync())
        await using (var transaction = await connection.BeginTransactionAsync())
        {
            await InsertRawAsync(connection, transaction, sku, 100);
            Assert.Equal(1, await CountBySkuPrefixAsync(connection, transaction, sku));
            await transaction.RollbackAsync();
        }

        await using var other = await PostgresTestDatabase.OpenVerifiedConnectionAsync();
        Assert.Equal(0, await CountBySkuPrefixAsync(other, null, sku));
    }
}
