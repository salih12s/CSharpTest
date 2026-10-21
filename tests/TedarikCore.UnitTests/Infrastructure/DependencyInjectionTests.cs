using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TedarikCore.Infrastructure;
using TedarikCore.Infrastructure.Persistence;

namespace TedarikCore.UnitTests.Infrastructure;

public class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_Registers_AppDbContext_Without_Opening_Connection()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=yok;Username=yok;Password=yok"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddInfrastructure(config);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Context oluşturmak bağlantı açmaz; sadece yapılandırmanın çözülebildiğini görürüz.
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    [Fact]
    public void AppDbContext_Without_ConnectionString_Throws_Clear_Error()
    {
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddInfrastructure(config);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var ex = Assert.Throws<InvalidOperationException>(
            () => scope.ServiceProvider.GetRequiredService<AppDbContext>());
        Assert.Contains("ConnectionStrings:Default", ex.Message);
    }
}
