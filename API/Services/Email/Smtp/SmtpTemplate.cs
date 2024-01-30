using Fluid;

namespace OpenShock.API.Services.Email.Smtp;

public sealed class SmtpTemplate
{
    private static readonly FluidParser FluidParser = new FluidParser();

    public required IFluidTemplate Subject { get; init; }
    public required IFluidTemplate Body { get; init; }

    public static async Task<SmtpTemplate> ParseFromFileThrow(string filePath)
    {
        var result = await ParseFromFile(filePath);
        if(result.IsT0) return result.AsT0;
        throw new InvalidDataException(result.AsT1);
    }
    
    public static Task<OneOf.OneOf<SmtpTemplate, string>> ParseFromFile(string filePath) =>
        ParseFromFile(File.OpenRead(filePath));
    
    public static async Task<OneOf.OneOf<SmtpTemplate, string>> ParseFromFile(FileStream fileStream)
    {
        using var streamReader = new StreamReader(fileStream);
        var subject = await streamReader.ReadLineAsync();
        if (subject == null) throw new InvalidDataException("Subject is null");

        if (!FluidParser.TryParse(subject, out var subjectTemplate, out var errorSubject)) return errorSubject;
        var body = await streamReader.ReadToEndAsync();
        if (!FluidParser.TryParse(body, out var bodyTemplate, out var errorBody)) return errorBody;

        return new SmtpTemplate
        {
            Subject = subjectTemplate,
            Body = bodyTemplate
        };
    }
}