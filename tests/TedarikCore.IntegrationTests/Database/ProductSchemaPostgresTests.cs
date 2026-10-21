using Npgsql;

namespace TedarikCore.IntegrationTests.Database;

// Migration'ın tedarikcore_test üzerinde gerçekten oluşturduğu yapıları sistem kataloglarından okur.
// Salt-okunurdur; migration uygulamaz. Migration uygulanmamışsa bu testler Failed olur (bilerek).
[Collection(PostgresTestCollection.Name)]
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
    public async Task Products_HasPrimaryKey_UniqueSkuIndex_PositivePriceCheck_And_UppercaseSkuCheck()
    {
        await using var connection = await PostgresTestDatabase.OpenVerifiedConnectionAsync();

        var constraints = await QueryAsync(connection,
            "select conname || ' ' || pg_get_constraintdef(oid) from pg_constraint " +
            "where conrelid = 'products'::regclass order by conname");
        var indexes = await QueryAsync(connection,
            "select indexname || ' ' || indexdef from pg_indexes where tablename = 'products' order by indexname");

        Assert.Contains("ck_products_price_kurus_positive CHECK ((price_kurus > 0))", constraints);
        Assert.Contains("ck_products_sku_uppercase CHECK (((sku)::text = upper((sku)::text)))", constraints);
        Assert.Contains("pk_products PRIMARY KEY (id)", constraints);
        Assert.Equal(3, constraints.Count); // başka beklenmeyen kısıt yok
        Assert.Contains(indexes, i => i.StartsWith("ix_products_sku ") && i.Contains("CREATE UNIQUE INDEX") && i.Contains("(sku)"));
    }

    [RequiresTestDatabaseFact]
    public async Task MigrationHistory_ContainsInitialCatalog_And_EnforceUppercaseSku_InOrder()
    {
        await using var connection = await PostgresTestDatabase.OpenVerifiedConnectionAsync();

        var applied = await QueryAsync(connection, "select \"MigrationId\" from \"__EFMigrationsHistory\" order by 1");

        Assert.Equal(2, applied.Count);
        Assert.EndsWith("_InitialCatalog", applied[0]);
        Assert.EndsWith("_EnforceUppercaseSku", applied[1]);
    }

    [RequiresTestDatabaseFact]
    public async Task Products_Table_HasNoLeftoverRows_AfterTests()
    {
        // Veri izolasyonunun kanıtı: testler (rollback ve tam-SKU temizliği) tabloda satır bırakmamalı.
        await using var connection = await PostgresTestDatabase.OpenVerifiedConnectionAsync();

        var rows = await QueryAsync(connection, "select sku from products order by sku limit 20");

        Assert.Empty(rows);
    }
}
