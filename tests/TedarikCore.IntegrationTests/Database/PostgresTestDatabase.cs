using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace TedarikCore.IntegrationTests.Database;

// Gerçek PostgreSQL testlerinin tek giriş kapısı. Yazma yapan hiçbir test bu sınıfın doğruladığı
// bağlantı dışında veritabanına ulaşamaz: ConnectionStrings:Test yoksa test atlanır (Skipped),
// varsa ama hedef 'tedarikcore_test' değilse ya da yerel makine değilse test hata verir (Failed)
// ve hiçbir şey yazılmaz.
internal static class PostgresTestDatabase
{
    public const string ExpectedDatabaseName = "tedarikcore_test";

    public static string? ConnectionString { get; } = new ConfigurationBuilder()
        .AddUserSecrets<Program>(optional: true)
        .AddEnvironmentVariables()
        .Build()
        .GetConnectionString("Test");

    // Saf fonksiyon: veritabanına bağlanmaz, bağlantı dizesinin güvenli olup olmadığını söyler.
    public static string? Validate(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return "ConnectionStrings:Test tanımlı değil.";
        }

        NpgsqlConnectionStringBuilder builder;
        try
        {
            builder = new NpgsqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException)
        {
            return "ConnectionStrings:Test okunamadı.";
        }

        if (!string.Equals(builder.Database, ExpectedDatabaseName, StringComparison.Ordinal))
        {
            return $"Hedef veritabanı '{builder.Database}'; yalnızca '{ExpectedDatabaseName}' kabul edilir.";
        }

        var host = builder.Host?.Trim();
        if (host is not ("localhost" or "127.0.0.1" or "::1"))
        {
            return $"Host '{host}' yerel değil; test veritabanı yalnızca bu makinede olabilir.";
        }

        return null;
    }

    public static Task<NpgsqlConnection> OpenVerifiedConnectionAsync() =>
        OpenAndVerifyAsync(new NpgsqlConnection(ConnectionString));

    // Verilen bağlantıyı güvenlik kurallarıyla doğrular, açar ve sunucunun gerçekten tedarikcore_test'e
    // bağlandığını SQL ile teyit eder. Herhangi bir adım başarısız olursa bağlantı dispose edilir
    // (bağlantı sızıntısı olmaz) ve hata olduğu gibi yeniden fırlatılır.
    internal static async Task<NpgsqlConnection> OpenAndVerifyAsync(NpgsqlConnection connection)
    {
        try
        {
            var problem = Validate(connection.ConnectionString);
            if (problem is not null)
            {
                throw new InvalidOperationException($"Yazma testi çalıştırılmadı: {problem}");
            }

            await connection.OpenAsync();

            await using var command = new NpgsqlCommand("select current_database()", connection);
            var actual = (string?)await command.ExecuteScalarAsync();
            if (actual != ExpectedDatabaseName)
            {
                throw new InvalidOperationException(
                    $"Yazma testi durduruldu: bağlanılan veritabanı '{actual}', beklenen '{ExpectedDatabaseName}'.");
            }

            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    // Temizlik: yalnızca verilen TAM SKU silinir (joker/önek yok), taze doğrulanmış bir bağlantıyla.
    // Silme sonrası satır kaldıysa hata verir; böylece kalıntı sessizce geçmez.
    public static async Task DeleteProductBySkuAsync(string sku)
    {
        await using var connection = await OpenVerifiedConnectionAsync();

        await using (var delete = new NpgsqlCommand("delete from products where sku = @sku", connection))
        {
            delete.Parameters.AddWithValue("sku", sku);
            await delete.ExecuteNonQueryAsync();
        }

        await using var count = new NpgsqlCommand("select count(*) from products where sku = @sku", connection);
        count.Parameters.AddWithValue("sku", sku);
        var remaining = (long)(await count.ExecuteScalarAsync())!;
        if (remaining != 0)
        {
            throw new InvalidOperationException($"Temizlik sonrası '{sku}' için {remaining} satır kaldı.");
        }
    }

    // Test gövdesini çalıştırır, SONRA her durumda temizliği çalıştırır. Hata kuralları:
    //  - yalnızca test başarısız: asıl hata, türü ve izi korunarak yeniden fırlatılır
    //  - yalnızca temizlik başarısız: temizlik hatası fırlatılır (sessizce yutulmaz)
    //  - ikisi de başarısız: ikisi de AggregateException içinde görünür (hiçbiri kaybolmaz)
    internal static async Task RunWithCleanupAsync(Func<Task> test, Func<Task> cleanup)
    {
        Exception? testFailure = null;
        try
        {
            await test();
        }
        catch (Exception e)
        {
            testFailure = e;
        }

        try
        {
            await cleanup();
        }
        catch (Exception cleanupFailure)
        {
            if (testFailure is null)
            {
                ExceptionDispatchInfo.Capture(cleanupFailure).Throw();
            }

            throw new AggregateException(
                "Test başarısız oldu ve temizlik de başarısız oldu; ikisi de iç hatalarda.",
                testFailure!,
                cleanupFailure);
        }

        if (testFailure is not null)
        {
            ExceptionDispatchInfo.Capture(testFailure).Throw();
        }
    }
}

// ConnectionStrings:Test hiç yoksa testi atlar. Var ama güvensizse atlamaz: test Failed olur ve
// nedenini yazar; böylece yanlış yapılandırma sessizce yeşil görünmez.
public sealed class RequiresTestDatabaseFactAttribute : FactAttribute
{
    public RequiresTestDatabaseFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(PostgresTestDatabase.ConnectionString))
        {
            Skip = "ConnectionStrings:Test tanımlı değil (User Secrets / ortam değişkeni); gerçek PostgreSQL testi atlandı.";
        }
    }
}

public sealed class RequiresTestDatabaseTheoryAttribute : TheoryAttribute
{
    public RequiresTestDatabaseTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(PostgresTestDatabase.ConnectionString))
        {
            Skip = "ConnectionStrings:Test tanımlı değil (User Secrets / ortam değişkeni); gerçek PostgreSQL testi atlandı.";
        }
    }
}
