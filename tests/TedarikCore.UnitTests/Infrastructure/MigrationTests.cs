using Microsoft.EntityFrameworkCore;
using TedarikCore.Infrastructure.Persistence;

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
    public void InitialCatalog_Migration_Exists()
    {
        using var context = CreateContext();

        Assert.Contains(context.Database.GetMigrations(), m => m.EndsWith("_InitialCatalog"));
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
}
