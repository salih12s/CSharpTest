namespace TedarikCore.IntegrationTests.Database;

// tedarikcore_test'e dokunan testler aynı koleksiyonda, yani sırayla çalışır. Böylece eşzamanlılık
// testinin geçici (commit edilmiş) satırı, "tabloda kalıntı yok" kontrolüyle yarışıp testi kararsız yapmaz.
public static class PostgresTestCollection
{
    public const string Name = "PostgreSQL test veritabani (seri)";
}

[CollectionDefinition(PostgresTestCollection.Name)]
public sealed class PostgresTestCollectionDefinition
{
}
