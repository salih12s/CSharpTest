using Microsoft.Extensions.DependencyInjection;

namespace TedarikCore.Infrastructure;

public static class DependencyInjection
{
    // Persistence ve dış servis kayıtları buraya eklenecek.
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        return services;
    }
}
