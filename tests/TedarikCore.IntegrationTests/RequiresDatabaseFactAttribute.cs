using Microsoft.Extensions.Configuration;

namespace TedarikCore.IntegrationTests;

// Gerçek PostgreSQL bağlantı dizesi (User Secrets veya ConnectionStrings__Default ortam
// değişkeni) yoksa testi atlar; böylece veritabanı olmayan makinede test kırmızıya dönmez,
// ama "atlandı" olarak raporda görünür.
public sealed class RequiresDatabaseFactAttribute : FactAttribute
{
    public RequiresDatabaseFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(TestDatabase.ConnectionString))
        {
            Skip = "ConnectionStrings:Default tanımlı değil (User Secrets / ortam değişkeni).";
        }
    }
}

internal static class TestDatabase
{
    public static string? ConnectionString { get; } = new ConfigurationBuilder()
        .AddUserSecrets<Program>(optional: true)
        .AddEnvironmentVariables()
        .Build()
        .GetConnectionString("Default");
}
