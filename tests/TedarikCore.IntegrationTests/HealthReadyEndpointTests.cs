using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace TedarikCore.IntegrationTests;

public class HealthReadyEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    // Port 1'de kimse dinlemez: bağlantı hemen reddedilir. Gerçek bir sır değildir.
    private const string UnreachableConnectionString =
        "Host=localhost;Port=1;Database=yok;Username=yok;Password=yok;Timeout=2";

    private readonly WebApplicationFactory<Program> _factory;

    public HealthReadyEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

    private WebApplicationFactory<Program> WithUnreachableDatabase() =>
        _factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = UnreachableConnectionString
            })));

    [Fact]
    public async Task Get_Health_Returns200_Even_When_Database_Unreachable()
    {
        using var factory = WithUnreachableDatabase();
        using var client = CreateClient(factory);

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_HealthReady_Returns503_When_Database_Unreachable()
    {
        using var factory = WithUnreachableDatabase();
        using var client = CreateClient(factory);

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Unhealthy", await response.Content.ReadAsStringAsync());
    }

    [RequiresDatabaseFact]
    public async Task Get_HealthReady_Returns200_When_Database_Reachable()
    {
        using var client = CreateClient(_factory);

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
