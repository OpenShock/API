using System.Text.Encodings.Web;
using Fluid;
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

    public static async Task<EmailTemplate> ParseFromFileThrow(string filePath)
    {
        var result = await ParseFromFile(filePath);
        return result.IsT0 ? result.AsT0 : throw new InvalidDataException(result.AsT1);
    }

    private static Task<OneOf.OneOf<EmailTemplate, string>> ParseFromFile(string filePath) =>
        ParseFromFile(File.OpenRead(filePath));

    private static async Task<OneOf.OneOf<EmailTemplate, string>> ParseFromFile(FileStream fileStream)
    {
        using var streamReader = new StreamReader(fileStream);
        var subject = await streamReader.ReadLineAsync();
        if (subject is null) throw new InvalidDataException("Subject is null");

        if (!Parser.TryParse(subject, out var subjectTemplate, out var errorSubject)) return errorSubject;
        var body = await streamReader.ReadToEndAsync();
        if (!Parser.TryParse(body, out var bodyTemplate, out var errorBody)) return errorBody;

        return new EmailTemplate
        {
            Subject = subjectTemplate,
            Body = bodyTemplate
        };
    }
}
