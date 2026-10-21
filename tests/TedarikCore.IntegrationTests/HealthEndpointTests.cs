using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TedarikCore.IntegrationTests;

public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // İstemci zaten https adresine gider ve yönlendirme takip edilmez;
    // böylece UseHttpsRedirection sonucu 307'ye çevirip testi bozamaz.
    private HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false
    });

    [Fact]
    public async Task Get_Health_Returns200_And_Healthy()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_UnknownRoute_Returns404()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/bu-yol-yok");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
