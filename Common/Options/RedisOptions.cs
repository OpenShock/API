using Microsoft.Extensions.Options;

namespace OpenShock.Common.Options;

public sealed class RedisOptions
{
    public const string SectionName = "OpenShock:Redis";

    public required string Conn { get; set; }
    public required string Host { get; init; } = string.Empty;
    public string User { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public short Port { get; init; } = 6379;
}

public sealed class RedisOptionsValidator : IValidateOptions<RedisOptions>
{
    public ValidateOptionsResult Validate(string? name, RedisOptions options)
    {
        ValidateOptionsResultBuilder builder = new ValidateOptionsResultBuilder();

        return builder.Build();
    }
}