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

    // Bağlantıyı açar ve sunucunun gerçekten tedarikcore_test'e bağlandığını SQL ile doğrular.
    public static async Task<NpgsqlConnection> OpenVerifiedConnectionAsync()
    {
        var problem = Validate(ConnectionString);
        if (problem is not null)
        {
            throw new InvalidOperationException($"Yazma testi çalıştırılmadı: {problem}");
        }

        var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand("select current_database()", connection);
        var actual = (string?)await command.ExecuteScalarAsync();
        if (actual != ExpectedDatabaseName)
        {
            await connection.DisposeAsync();
            throw new InvalidOperationException(
                $"Yazma testi durduruldu: bağlanılan veritabanı '{actual}', beklenen '{ExpectedDatabaseName}'.");
        }

        return connection;
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
