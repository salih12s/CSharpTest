namespace TedarikCore.IntegrationTests.Database;

// Güvenlik korumasının kendisi test edilir. Bu testler veritabanına bağlanmaz, her makinede çalışır.
public class PostgresTestDatabaseGuardTests
{
    [Fact]
    public void Validate_AcceptsOnlyLocal_TedarikcoreTest_Database()
    {
        var problem = PostgresTestDatabase.Validate(
            "Host=localhost;Port=5432;Database=tedarikcore_test;Username=u;Password=p");

        Assert.Null(problem);
    }

    [Theory]
    [InlineData("Host=127.0.0.1;Database=tedarikcore_test;Username=u;Password=p")]
    [InlineData("Host=::1;Database=tedarikcore_test;Username=u;Password=p")]
    public void Validate_AcceptsOtherLocalHostNames(string connectionString)
    {
        Assert.Null(PostgresTestDatabase.Validate(connectionString));
    }

    [Theory]
    [InlineData("Host=localhost;Database=tedarikcore_dev;Username=u;Password=p")]
    [InlineData("Host=localhost;Database=postgres;Username=u;Password=p")]
    [InlineData("Host=localhost;Database=tedarikcore_test2;Username=u;Password=p")]
    [InlineData("Host=localhost;Database=TEDARIKCORE_TEST;Username=u;Password=p")]
    [InlineData("Host=localhost;Database=tedarikcore_tes;Username=u;Password=p")]
    [InlineData("Host=localhost;Username=u;Password=p")]
    public void Validate_RejectsAnyOtherDatabase(string connectionString)
    {
        Assert.NotNull(PostgresTestDatabase.Validate(connectionString));
    }

    [Theory]
    [InlineData("Host=db.example.com;Database=tedarikcore_test;Username=u;Password=p")]
    [InlineData("Host=10.0.0.5;Database=tedarikcore_test;Username=u;Password=p")]
    public void Validate_RejectsRemoteHosts(string connectionString)
    {
        Assert.NotNull(PostgresTestDatabase.Validate(connectionString));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("bu bir bağlantı dizesi değil")]
    public void Validate_RejectsMissingOrBrokenConnectionString(string? connectionString)
    {
        Assert.NotNull(PostgresTestDatabase.Validate(connectionString));
    }
}
