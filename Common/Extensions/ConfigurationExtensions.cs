﻿using Microsoft.Extensions.Options;
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
        builder.Services.AddSingleton<IValidateOptions<DatabaseOptions>, DatabaseOptionsValidator>();
        
        builder.Services.Configure<RedisOptions>(builder.Configuration.GetRequiredSection(RedisOptions.SectionName));
        builder.Services.AddSingleton<IValidateOptions<RedisOptions>, RedisOptionsValidator>();
        
        builder.Services.Configure<FrontendOptions>(builder.Configuration.GetRequiredSection(FrontendOptions.SectionName));
        builder.Services.AddSingleton<IValidateOptions<FrontendOptions>, FrontendOptionsValidator>();
        
        builder.Services.Configure<MetricsOptions>(builder.Configuration.GetSection(MetricsOptions.SectionName));
        builder.Services.AddSingleton<IValidateOptions<MetricsOptions>, MetricsOptionsValidator>();

        return builder;
    }
}
