using Npgsql;

namespace TedarikCore.IntegrationTests.Database;

// ck_products_sku_uppercase (sku = upper(sku)) kuralını C# ToUpperInvariant() ve PostgreSQL upper()
// birlikte uygular. Bu test, Product.Create ile normalize edilmiş HİÇBİR karakterin veritabanı tarafından
// "küçük harf" sayılıp reddedilmediğini tüm Unicode için ölçer. PostgreSQL'in yerel ayarı (kolasyon/ctype)
// değişirse (ör. farklı bir sunucu) bu test uyumsuzluğu hemen gösterir.
//
// Salt-okunurdur: yalnızca "select ... upper(...)" çalıştırır, hiçbir şey yazmaz.
[Collection(PostgresTestCollection.Name)]
public class SkuUppercaseCompatibilityPostgresTests
{
    [RequiresTestDatabaseFact]
    public async Task CSharpToUpperInvariant_Output_IsNeverRejected_By_PostgresUppercaseCheck_ForAllUnicode()
    {
        await using var connection = await PostgresTestDatabase.OpenVerifiedConnectionAsync();

        var rejected = new List<string>();
        var batch = new List<string>(20_000);
        var scanned = 0;

        async Task FlushAsync()
        {
            if (batch.Count == 0)
            {
                return;
            }

            // CHECK ifadesinin kendisi: sku = upper(sku). Bu ifadeyi SAĞLAMAYAN değerler reddedilirdi.
            await using var command = new NpgsqlCommand(
                "select v from unnest(@values::text[]) as t(v) where v <> upper(v)", connection);
            command.Parameters.AddWithValue("values", batch.ToArray());
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var value = reader.GetString(0);
                rejected.Add($"U+{char.ConvertToUtf32(value, 0):X4}");
            }

            scanned += batch.Count;
            batch.Clear();
        }

        for (var codePoint = 1; codePoint <= 0x10FFFF; codePoint++)
        {
            if (codePoint is >= 0xD800 and <= 0xDFFF)
            {
                continue; // yalnız vekil (surrogate) yarıları geçerli metin değildir
            }

            batch.Add(char.ConvertFromUtf32(codePoint).ToUpperInvariant());
            if (batch.Count == 20_000)
            {
                await FlushAsync();
            }
        }

        await FlushAsync();

        Assert.True(scanned > 1_000_000, $"Beklenenden az kod noktası tarandı: {scanned}");
        Assert.True(
            rejected.Count == 0,
            $"C# ToUpperInvariant çıktısı PostgreSQL upper() ile uyuşmuyor ({rejected.Count} karakter): " +
            string.Join(", ", rejected.Take(20)));
    }
}
