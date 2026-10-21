using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TedarikCore.Infrastructure.Persistence;

namespace TedarikCore.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Default";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Bağlantı dizesi ilk DbContext kullanımında okunur; yoksa uygulama yine açılır,
        // yalnızca veritabanına ihtiyaç duyan işler (ör. /health/ready) başarısız olur.
        services.AddDbContext<AppDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"ConnectionStrings:{ConnectionStringName} tanımlı değil. " +
                    "User Secrets veya ortam değişkeni ile verin (bkz. CLAUDE.md).");

            options.UseNpgsql(connectionString);
        });

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>("database", tags: [HealthCheckTags.Ready]);

        return services;
    }
}
