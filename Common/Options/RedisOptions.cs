using Microsoft.Extensions.Options;

namespace OpenShock.Common.Options;

public sealed class RedisOptions
{
    public const string SectionName = "OpenShock:Redis";

    public required string Conn { get; set; }
    public required string Host { get; init; } = string.Empty;
    public string User { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public ushort Port { get; init; } = 6379;
}

public sealed class RedisOptionsValidator : IValidateOptions<RedisOptions>
{
    public ValidateOptionsResult Validate(string? name, RedisOptions options)
    {
        ValidateOptionsResultBuilder builder = new ValidateOptionsResultBuilder();

        if (string.IsNullOrEmpty(options.Conn))
        {
            if (string.IsNullOrEmpty(options.Host)) builder.AddError("Host field is required if no connectionstring is specified", nameof(options.Host));
            if (string.IsNullOrEmpty(options.User)) builder.AddError("User field is required if no connectionstring is specified", nameof(options.Host));
            if (!string.IsNullOrEmpty(options.Password)) builder.AddError("Password field is required if no connectionstring is specified", nameof(options.Host));
        }

        return builder.Build();
    }
}