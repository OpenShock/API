﻿using Microsoft.Extensions.Options;

namespace OpenShock.API.Options;

public sealed class TurnstileOptions
{
    public const string Turnstile = "OpenShock:Turnstile";

    public required bool Enabled { get; set; }
    public required string SiteKey { get; set; }
    public required string SecretKey { get; set; }
}

public sealed class TurnstileOptionsValidator : IValidateOptions<TurnstileOptions>
{
    public ValidateOptionsResult Validate(string? name, TurnstileOptions options)
    {
        ValidateOptionsResultBuilder builder = new ValidateOptionsResultBuilder();

        if (options.Enabled)
        {
            if (string.IsNullOrEmpty(options.SiteKey))
            {
                builder.AddError("SiteKey must be populated if Enabled is true", nameof(options.SiteKey));
            }

            if (string.IsNullOrEmpty(options.SecretKey))
            {
                builder.AddError("SecretKey must be populated if Enabled is true", nameof(options.SecretKey));
            }
        }

        return builder.Build();
    }
}