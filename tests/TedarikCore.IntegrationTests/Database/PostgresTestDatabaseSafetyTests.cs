using Npgsql;

namespace TedarikCore.IntegrationTests.Database;

// Test altyapısının kendi güvenilirliği. Veritabanına BAĞLANMAZ (port 1'de kimse dinlemez),
// bu yüzden her makinede çalışır.
public class PostgresTestDatabaseSafetyTests
{
    private const string UnreachableTestDb =
        "Host=localhost;Port=1;Database=tedarikcore_test;Username=yok;Password=yok;Timeout=1";

    [Fact]
    public async Task OpenAndVerify_WhenConnectionCannotBeOpened_DisposesConnection_And_Rethrows()
    {
        var connection = new NpgsqlConnection(UnreachableTestDb);

        await Assert.ThrowsAnyAsync<Exception>(() => PostgresTestDatabase.OpenAndVerifyAsync(connection));

        // Dispose edilmiş bağlantı tekrar açılamaz.
        await Assert.ThrowsAsync<ObjectDisposedException>(() => connection.OpenAsync());
    }

    [Fact]
    public async Task OpenAndVerify_WhenTargetDatabaseIsNotTest_DisposesConnection_WithoutConnecting()
    {
        var connection = new NpgsqlConnection(
            "Host=localhost;Port=1;Database=tedarikcore_dev;Username=yok;Password=yok;Timeout=1");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => PostgresTestDatabase.OpenAndVerifyAsync(connection));

        Assert.Contains("yalnızca 'tedarikcore_test' kabul edilir", ex.Message);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => connection.OpenAsync());
    }

    [Fact]
    public async Task RunWithCleanup_WhenBothSucceed_Completes_And_RunsCleanup()
    {
        var cleaned = false;

        await PostgresTestDatabase.RunWithCleanupAsync(() => Task.CompletedTask, () =>
        {
            cleaned = true;
            return Task.CompletedTask;
        });

        Assert.True(cleaned);
    }

    [Fact]
    public async Task RunWithCleanup_WhenOnlyTestFails_RethrowsOriginal_And_StillCleansUp()
    {
        var cleaned = false;
        var original = new InvalidOperationException("asıl test hatası");

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            PostgresTestDatabase.RunWithCleanupAsync(() => throw original, () =>
            {
                cleaned = true;
                return Task.CompletedTask;
            }));

        Assert.Same(original, thrown); // tür ve örnek korunur
        Assert.True(cleaned);
    }

    [Fact]
    public async Task RunWithCleanup_WhenOnlyCleanupFails_ThrowsCleanupFailure_NotSwallowed()
    {
        var cleanupFailure = new InvalidOperationException("temizlik hatası");

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            PostgresTestDatabase.RunWithCleanupAsync(() => Task.CompletedTask, () => throw cleanupFailure));

        Assert.Same(cleanupFailure, thrown);
    }

    [Fact]
    public async Task RunWithCleanup_WhenBothFail_KeepsBothFailures_Visible()
    {
        var testFailure = new InvalidOperationException("asıl test hatası");
        var cleanupFailure = new InvalidOperationException("temizlik hatası");

        var thrown = await Assert.ThrowsAsync<AggregateException>(() =>
            PostgresTestDatabase.RunWithCleanupAsync(() => throw testFailure, () => throw cleanupFailure));

        Assert.Equal(2, thrown.InnerExceptions.Count);
        Assert.Contains(testFailure, thrown.InnerExceptions);
        Assert.Contains(cleanupFailure, thrown.InnerExceptions);
    }
}
