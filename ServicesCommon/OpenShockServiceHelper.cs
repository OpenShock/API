using OpenShock.ServicesCommon.Services.BatchUpdate;

namespace OpenShock.ServicesCommon;

public static class OpenShockServiceHelper
{
    /// <summary>
    /// Register all OpenShock services for PRODUCTION use
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddOpenShockServices(this IServiceCollection services)
    {
        services.AddSingleton<IBatchUpdateService, BatchUpdateService>();
        services.AddHostedService<BatchUpdateService>(provider => (BatchUpdateService)provider.GetRequiredService<IBatchUpdateService>());

        return services;
    }
}