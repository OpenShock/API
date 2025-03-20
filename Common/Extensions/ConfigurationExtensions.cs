using OpenShock.Common.Options;

namespace OpenShock.Common.Extensions;

public static class ConfigurationExtensions
{
    public static WebApplicationBuilder RegisterCommonOpenShockOptions(this WebApplicationBuilder builder)
    {
#if DEBUG
        Console.WriteLine(builder.Configuration.GetDebugView());
#endif
        builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetRequiredSection(DatabaseOptions.SectionName));
        builder.Services.Configure<RedisOptions>(builder.Configuration.GetRequiredSection(RedisOptions.SectionName));
        builder.Services.Configure<FrontendOptions>(builder.Configuration.GetRequiredSection(FrontendOptions.SectionName));
        builder.Services.Configure<MetricsOptions>(builder.Configuration.GetSection(MetricsOptions.SectionName));

        return builder;
    }
}
