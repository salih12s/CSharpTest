using Npgsql;

namespace TedarikCore.IntegrationTests.Database;

// Migration'ın tedarikcore_test üzerinde gerçekten oluşturduğu yapıları sistem kataloglarından okur.
// Salt-okunurdur; migration uygulamaz. Migration uygulanmamışsa bu testler Failed olur (bilerek).
public class ProductSchemaPostgresTests
{
    private static async Task<List<string>> QueryAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<string>();
        while (await reader.ReadAsync())
        {
            rows.Add(reader.GetString(0));
        }

        return rows;
    }

    [RequiresTestDatabaseFact]
    public async Task OnlyProductsAndMigrationHistory_TablesExist()
    {
        await using var connection = await PostgresTestDatabase.OpenVerifiedConnectionAsync();

        var tables = await QueryAsync(connection,
            "select table_name from information_schema.tables " +
            "where table_schema not in ('pg_catalog','information_schema') order by 1");

        Assert.Equal(["__EFMigrationsHistory", "products"], tables);
    }

    [RequiresTestDatabaseFact]
    public async Task Products_HasExpectedColumns_Types_And_Nullability()
    {
        await using var connection = await PostgresTestDatabase.OpenVerifiedConnectionAsync();

        var columns = await QueryAsync(connection,
            "select column_name || ' ' || data_type || ' ' || coalesce(character_maximum_length::text, '-') " +
            "|| ' ' || is_nullable from information_schema.columns where table_name = 'products' order by ordinal_position");

        Assert.Equal(
        [
            "id uuid - NO",
            "sku character varying 64 NO",
            "name character varying 200 NO",
            "price_kurus bigint - NO",
            "is_active boolean - NO"
        ],
        columns);
    }

    [RequiresTestDatabaseFact]
    public async Task Products_HasPrimaryKey_UniqueSkuIndex_And_PositivePriceCheck()
    {
        await using var connection = await PostgresTestDatabase.OpenVerifiedConnectionAsync();

        var constraints = await QueryAsync(connection,
            "select conname || ' ' || pg_get_constraintdef(oid) from pg_constraint " +
            "where conrelid = 'products'::regclass order by conname");
        var indexes = await QueryAsync(connection,
            "select indexname || ' ' || indexdef from pg_indexes where tablename = 'products' order by indexname");

        Assert.Contains("ck_products_price_kurus_positive CHECK ((price_kurus > 0))", constraints);
        Assert.Contains("pk_products PRIMARY KEY (id)", constraints);
        Assert.Contains(indexes, i => i.StartsWith("ix_products_sku ") && i.Contains("CREATE UNIQUE INDEX") && i.Contains("(sku)"));
    }

    [RequiresTestDatabaseFact]
    public async Task MigrationHistory_ContainsInitialCatalog()
    {
        await using var connection = await PostgresTestDatabase.OpenVerifiedConnectionAsync();

        var applied = await QueryAsync(connection, "select \"MigrationId\" from \"__EFMigrationsHistory\" order by 1");

        var only = Assert.Single(applied);
        Assert.EndsWith("_InitialCatalog", only);
    }
}
