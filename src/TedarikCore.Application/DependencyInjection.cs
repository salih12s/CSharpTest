using Microsoft.Extensions.DependencyInjection;

namespace TedarikCore.Application;

public static class DependencyInjection
{
    // Use case / service kayıtları buraya eklenecek.
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
