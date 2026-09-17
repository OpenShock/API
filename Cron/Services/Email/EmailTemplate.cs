using System.Diagnostics;
using System.Text.Encodings.Web;
using Fluid;
using OpenShock.Common.Results;
using OpenShock.Cron.Services.Email.Mailjet.Mail;

namespace OpenShock.Cron.Services.Email;

public sealed class EmailTemplate
{
    private static readonly FluidParser Parser = new();
    private static readonly TemplateOptions Options = CreateOptions();

    private static TemplateOptions CreateOptions()
    {
        var options = new TemplateOptions();
        options.MemberAccessStrategy.Register<Contact>();
        return options;
    }

    public required IFluidTemplate Subject { get; init; }
    public required IFluidTemplate Body { get; init; }

    public async Task<(string Subject, string HtmlBody)> RenderAsync<T>(T data)
    {
        var context = new TemplateContext(data, Options);
        var subject = await Subject.RenderAsync(context);
        var htmlBody = await Body.RenderAsync(context, HtmlEncoder.Default);
        return (subject, htmlBody);
    }

    public static async Task<EmailTemplate> ParseFromFileOrThrow(string filePath)
    {
        var result = await ParseFromFile(filePath);
        return result switch
        {
            EmailTemplate template => template,
            TemplateParseError error => throw new InvalidDataException(error.Value),
            _ => throw new UnreachableException()
        };
    }

    public static Task<Union2<EmailTemplate, TemplateParseError>> ParseFromFile(string filePath) =>
        ParseFromFile(File.OpenRead(filePath));

    private static async Task<Union2<EmailTemplate, TemplateParseError>> ParseFromFile(FileStream fileStream)
    {
        using var streamReader = new StreamReader(fileStream);
        var subject = await streamReader.ReadLineAsync();
        if (subject is null) return new TemplateParseError("Subject is null");

        if (!Parser.TryParse(subject, out var subjectTemplate, out var errorSubject)) return new TemplateParseError(errorSubject);
        var body = await streamReader.ReadToEndAsync();
        if (!Parser.TryParse(body, out var bodyTemplate, out var errorBody)) return new TemplateParseError(errorBody);

        return new EmailTemplate
        {
            Subject = subjectTemplate,
            Body = bodyTemplate
        };
    }
}

/// <summary>
/// Union case for when a Fluid template fails to parse
/// </summary>
public sealed record TemplateParseError(string Value);
