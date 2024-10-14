using System.Diagnostics.CodeAnalysis;
using OneOf;
using OneOf.Types;

namespace OpenShock.Common.Validation;

public static class UsernameValidator
{
    public static OneOf<Success, UsernameError> Validate(string username)
    {
        if (username.Length < ValidationConstants.UsernameMinLength)
        {
            return new UsernameError(UsernameErrorType.TooShort, "Username is too short.");
        }

        if (username.Length > ValidationConstants.UsernameMaxLength)
        {
            return new UsernameError(UsernameErrorType.TooLong, "Username is too long.");
        }

        if (char.IsWhiteSpace(username, 0) || char.IsWhiteSpace(username, username.Length - 1))
        {
            return new UsernameError(UsernameErrorType.StartOrEndWithWhitespace, "Username cannot start or end with whitespace.");
        }

        if (username.Contains('@'))
        {
            return new UsernameError(UsernameErrorType.ResembleEmail, "Username cannot resemble an email address.");
        }

        // Check if string contains any unwanted characters
        if (CharsetMatchers.ContainsUndesiredUserInterfaceCharacters(username))
            return new UsernameError(UsernameErrorType.ObnoxiousCharacters, "Username must not contain obnoxious characters.");

        return new Success();
    }
}

[method: SetsRequiredMembers]
public readonly struct UsernameError(UsernameErrorType type, string message)
{
    public required string Message { get; init; } = message;
    public required UsernameErrorType Type { get; init; } = type;
}

public enum UsernameErrorType
{
    TooShort,
    TooLong,
    StartOrEndWithWhitespace,
    ResembleEmail,
    ObnoxiousCharacters
}